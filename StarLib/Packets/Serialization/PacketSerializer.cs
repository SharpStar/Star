// SharpStar. A Starbound wrapper.
// Copyright (C) 2015 Mitchell Kutchuk
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StarLib.DataTypes.Variant;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Networking;
using StarLib.Packets.Serialization.Attributes;

namespace StarLib.Packets.Serialization
{
	public class PacketSerializer
	{
		public static readonly SerializableType[] Serializables =
		{
			new StarSerializableType<int>((writer, val) => writer.Write(val), reader => reader.ReadInt32()),
			new StarSerializableType<uint>((writer, val) => writer.Write(val), reader => reader.ReadUInt32()),
			new StarSerializableType<short>((writer, val) => writer.Write(val), reader => reader.ReadInt16()),
			new StarSerializableType<ushort>((writer, val) => writer.Write(val), reader => reader.ReadUInt16()),
			new StarSerializableType<byte>((writer, val) => writer.Write(val), reader => reader.ReadByte()),
			new StarSerializableType<bool>((writer, val) => writer.Write(val), reader => reader.ReadBoolean()),
			new StarSerializableType<float>((writer, val) => writer.Write(val), reader => reader.ReadSingle()),
			new StarSerializableType<double>((writer, val) => writer.Write(val), reader => reader.ReadDouble()),
			new StarSerializableType<long>((writer, val) => writer.Write(val), reader => reader.ReadInt64()),
			new StarSerializableType<ulong>((writer, val) => writer.Write(val), reader => reader.ReadVLQ()),
			new StarSerializableType<string>((writer, val) => writer.WriteStarString(val), reader => reader.ReadString()),
			new StarSerializableType<byte[]>((writer, val) => writer.WriteUInt8Array(val), reader => reader.ReadUInt8Array()),
			new StarSerializableType<StarVariant>((writer, val) => writer.WriteVariant(val), reader => reader.ReadVariant()),
			new StarSerializableType<VariantDictionary>((writer, val) => writer.WriteVariant(new StarVariant(new VariantValue(val))), reader => reader.ReadVariant().Value)
		};

		public static Dictionary<Type, Tuple<Action<StarWriter, object>, Func<StarReader, object>>> PacketSerializers;

		private static readonly Dictionary<Type, SerializableType> _serializables = Serializables.ToDictionary(p => p.Type);

		private static readonly Func<StarReader, int> _collectionLengthReader = reader => (int)reader.ReadVLQ();
		private static readonly Action<StarWriter, int> _collectionLengthWriter = (writer, i) => writer.WriteVlq((ulong)i);

		private static readonly ConstantExpression _isStreamOver = Expression.Constant(new Func<StarReader, bool>(s => s.BaseStream.Length == s.BaseStream.Position));
		private static readonly Expression _collectionLengthReaderExpr = Expression.Constant(_collectionLengthReader);
		private static readonly Expression _collectionLengthWriterExpr = Expression.Constant(_collectionLengthWriter);

		static PacketSerializer()
		{
			PacketSerializers = new Dictionary<Type, Tuple<Action<StarWriter, object>, Func<StarReader, object>>>();
		}

		/// <summary>
		///     Gets or builds a serialization expression tree to convert the specified instance to an array of bytes.
		/// </summary>
		public static byte[] Serialize(object value)
		{
			using (var writer = new StarWriter())
			{
				Serialize(value, writer);

				return writer.ToArray();
			}
		}

		public static void BuildAndStore(Type type)
		{
			if (!PacketSerializers.ContainsKey(type))
				PacketSerializers.Add(type, Tuple.Create(BuildSerializer(type), BuildDeserializer(type)));
		}

		static void Serialize(object value, StarWriter writer)
		{
			try
			{
				Type type = value.GetType();

				BuildAndStore(type);

				var lambda = PacketSerializers[type];

				lambda.Item1(writer, value);
			}
			catch (Exception e)
			{
				throw new ApplicationException(string.Format("Serialization error. Type: {0}", value.GetType()), e);
			}
		}

		/// <summary>
		///     Gets or builds a deserialization expression tree to convert the specified <see cref="StarReader" /> to an instance of
		///     the specified type.
		/// </summary>
		public static object Deserialize(StarReader reader, Type type)
		{
			object result = DeserializeInternal(reader, type);

			if (reader.DataLeft != 0)
				StarLog.DefaultLogger.Warn("Packet {0} is incomplete!", type.FullName);

			return result;
		}

		private static object DeserializeInternal(StarReader reader, Type type)
		{
			try
			{
				BuildAndStore(type);

				var lambda = PacketSerializers[type];

				return lambda.Item2(reader);
			}
			catch (Exception e)
			{
				throw new ApplicationException(string.Format("Serialization error. Type: {0}", type), e);
			}
		}

		public static T Deserialize<T>(StarReader reader) where T : Packet
		{
			T result = Deserialize(reader, typeof(T)) as T;

			if (reader.DataLeft != 0)
				StarLog.DefaultLogger.Warn("Packet {0} is incomplete!", typeof(T).FullName);

			return result;
		}

		static Action<StarWriter, object> BuildSerializer(Type arg)
		{
			//To store the lambdas in the same dictionary the parameter is boxed and unboxed in runtime.
			var boxedInstance = Expression.Parameter(typeof(object), "boxedInstance");
			var instance = Expression.Variable(arg, "instance");
			var dest = Expression.Parameter(typeof(StarWriter), "dest");
			var block = Serialize(arg, dest, instance, boxedInstance);
			var lambda = Expression.Lambda<Action<StarWriter, object>>(block, dest, boxedInstance);
			return lambda.Compile();
		}

		static Func<StarReader, object> BuildDeserializer(Type arg)
		{
			var instance = Expression.Variable(arg, "instance");
			var source = Expression.Parameter(typeof(StarReader), "source");
			var block = Deserialize(instance, source);
			var lambda = Expression.Lambda<Func<StarReader, object>>(block, source);
			return lambda.Compile();
		}

		static BlockExpression Serialize(Type arg, Expression dest,
			ParameterExpression instance, Expression boxedInstance)
		{
			return Expression.Block(new[] { instance },
				Expression.Assign(instance, Expression.Convert(boxedInstance, arg)),
				WriteToBuffer(instance, arg, dest),
				dest);
		}

		static BlockExpression Deserialize(ParameterExpression instance, Expression stream)
		{
			return Expression.Block(new[] { instance },
				Expression.Assign(instance, Expression.New(instance.Type)),
				ReadFromBuffer(instance, stream),
				Expression.Convert(instance, typeof(object)));
		}

		static BlockExpression WriteToBuffer(Expression instance, Type type, Expression dest)
		{
			var members = GetMembers(type).ToArray();
			var expressions = new List<Expression>();

			bool skipOne = false;
			for (int i = 0; i < members.Length; i++)
			{
				if (skipOne)
				{
					skipOne = false;
					continue;
				}

				var member = Expression.Property(instance, members[i].Item1);

				if (IsConditional(member) && member.Type == typeof(bool))
				{
					var write = Expression.Variable(typeof(bool), "write");

					var newExpr = Expression.Block(new[] { write },
							WriteOne(dest, member, type, members[i].Item2),
							Expression.Assign(write, member),
							Expression.IfThen(Expression.Equal(write, Expression.Constant(true)),
								WriteOne(dest, Expression.Property(instance, members[i + 1].Item1), type, members[i + 1].Item2)
							)
						);

					skipOne = true;

					expressions.Add(newExpr);
				}
				else if (IsAnyType(member))
				{
					expressions.Add(WriteAnyType(member, dest));
				}
				//else if (IsMaybeType(member))
				//{
				//	expressions.Add(WriteMaybeType(member, dest));
				//}
				else if (IsEitherType(member))
				{
					expressions.Add(WriteEitherType(member, dest));
				}
				else
				{
					expressions.Add(WriteOne(dest, member, member.Type, members[i].Item2));
				}

			}

			return expressions.Any() ? Expression.Block(expressions) : Expression.Block(Expression.Empty());
			//return Expression.Block(GetMembers(instance)
			//    .Select(member => WriteOne(dest, member)));
		}

		static BlockExpression WriteEitherType(MemberExpression member, Expression dest)
		{
			var eitherIndex = member.Type.GetProperty("Index");
			var eitherLeft = member.Type.GetProperty("Left");
			var eitherRight = member.Type.GetProperty("Right");

			var eitherTypes = member.Type.GetGenericArguments();
			var leftType = eitherTypes[0];
			var rightType = eitherTypes[1];

			var leftMaybeType = typeof(Maybe<>).MakeGenericType(leftType);
			var rightMaybeType = typeof(Maybe<>).MakeGenericType(rightType);

			var left = Expression.Variable(leftMaybeType, "left");
			var right = Expression.Variable(rightMaybeType, "right");

			var maybeLeftVal = leftMaybeType.GetProperty("Value");
			var maybeRightVal = rightMaybeType.GetProperty("Value");

			var newExpr = Expression.Block(new[] { left, right },
				Expression.Assign(left, Expression.Property(member, eitherLeft)),
				Expression.Assign(right, Expression.Property(member, eitherRight)),
				Expression.IfThenElse(
					Expression.NotEqual(Expression.Property(left, maybeLeftVal), Expression.Constant(null)),
					Expression.Block(
						Expression.Assign(Expression.Property(member, eitherIndex), Expression.Constant((byte)1)),
						WriteOne(dest, Expression.Property(member, eitherIndex), typeof(byte), null),
						WriteMaybeType(Expression.Property(member, eitherLeft), dest)
					),
					Expression.IfThenElse(
						Expression.NotEqual(Expression.Property(right, maybeRightVal), Expression.Constant(null)),
						Expression.Block(
							Expression.Assign(Expression.Property(member, eitherIndex), Expression.Constant((byte)2)),
							WriteOne(dest, Expression.Property(member, eitherIndex), typeof(byte), null),
							WriteMaybeType(Expression.Property(member, eitherRight), dest)
						),
						Expression.Block(
							Expression.Assign(Expression.Property(member, eitherIndex), Expression.Constant((byte)0)),
							WriteOne(dest, Expression.Property(member, eitherIndex), typeof(byte), null)
						)
					)
				)
			);

			return newExpr;
		}

		static BlockExpression WriteMaybeType(MemberExpression member, Expression dest)
		{
			var maybeValueProp = member.Type.GetProperty("Value");

			Type maybeType = member.Type.GetGenericArguments()[0];

			var newExpr = Expression.Block(
				Expression.IfThen(Expression.NotEqual(Expression.Property(member, maybeValueProp), Expression.Constant(null)),
					WriteOne(dest, Expression.Property(member, maybeValueProp), maybeType, null)
				)
			);

			return newExpr;
		}

		static BlockExpression WriteAnyType(MemberExpression member, Expression dest)
		{
			var exit = Expression.Label();

			var anyIndexProp = member.Type.GetProperty("Index");
			var anyValueProp = member.Type.GetProperty("Value");
			var valType = typeof(object).GetMethod("GetType");

			var typeExpr = Expression.Variable(typeof(Type), "selType");

			var valProp = Expression.Property(member, anyValueProp);

			var ctr = Expression.Variable(typeof(int), "ctr");

			Type[] args = member.Type.GetGenericArguments();

			var gTypes = Expression.Constant(args, typeof(Type[]));

			var exprs = new List<Expression>();
			foreach (Type t in args)
			{
				var expr = Expression.Block(new[] { typeExpr, ctr },
					Expression.Assign(typeExpr, Expression.Call(valProp, valType)),
					Expression.Loop(Expression.Block(
							Expression.IfThenElse(Expression.LessThan(ctr, Expression.ArrayLength(gTypes)),
								Expression.Block(
									Expression.IfThen(
										Expression.AndAlso(
										Expression.Equal(typeExpr, Expression.ArrayIndex(gTypes, ctr)),
										Expression.Equal(typeExpr, Expression.Constant(t))),
											Expression.Block(
												Expression.Assign(Expression.Property(member, anyIndexProp),
													Expression.Convert(Expression.Increment(ctr), typeof(byte))
												),
												WriteOne(dest, Expression.Property(member, anyIndexProp), typeof(byte), null),
												WriteOne(dest, valProp, t, null),
												Expression.Break(exit)
											)
										),
										Expression.PostIncrementAssign(ctr)
									),
								Expression.Break(exit))
							),
						exit)
				);

				exprs.Add(expr);
			}

			var newExpr = Expression.Block(
					Expression.IfThenElse(Expression.NotEqual(valProp, Expression.Constant(null)),
						Expression.Block(exprs),
						Expression.Invoke(Expression.Constant(GetSerializableType(typeof(byte)).Serializer), dest, Expression.Constant((byte)0))
				)
			);

			return newExpr;
		}

		static BlockExpression ReadFromBuffer(Expression instance, Expression stream)
		{
			var members = GetMembers(instance.Type).ToArray();
			var expressions = new List<Expression>();

			bool skipOne = false;
			for (int i = 0; i < members.Length; i++)
			{
				if (skipOne)
				{
					skipOne = false;
					continue;
				}

				var member = Expression.Property(instance, members[i].Item1);

				if (IsConditional(member) && member.Type == typeof(bool))
				{
					var read = Expression.Variable(member.Type, "read");
					var next = Expression.Property(instance, members[i + 1].Item1);

					var newExpr = Expression.Block(new[] { read },
					Expression.Assign(read, ReadOne(stream, member, member.Type, members[i].Item2)),
						Expression.IfThen(
							Expression.Equal(read, Expression.Constant(true)),
								ReadOne(stream, Expression.Property(instance, members[i + 1].Item1), next.Type, members[i + 1].Item2)
						)
					);

					skipOne = true;

					expressions.Add(newExpr);
				}
				else if (IsAnyType(member))
				{
					expressions.Add(ReadAnyType(member, stream, members[i].Item2));
				}
				else if (IsEitherType(member))
				{
					expressions.Add(ReadEitherType(member, stream, members[i].Item2));
				}
				else
				{
					expressions.Add(ReadOne(stream, member, member.Type, members[i].Item2));
				}
			}

			return expressions.Any() ? Expression.Block(expressions) : Expression.Block(Expression.Empty());
			//return Expression.Block(GetMembers(instance)
			//    .Select(member => ReadOne(stream, member)));
		}

		static BlockExpression ReadMaybeType(MemberExpression member, Expression stream, StarSerializeAttribute attrib)
		{
			var maybe = Expression.Variable(member.Type, "type");

			var newExpr = Expression.Block(new[] { maybe },
				Expression.Assign(maybe, ReadOne(stream, member, member.Type, attrib)),
				member
			);

			return newExpr;
		}

		static BlockExpression ReadEitherType(MemberExpression member, Expression stream, StarSerializeAttribute attrib)
		{
			var eitherLeft = member.Type.GetProperty("Left");
			var eitherRight = member.Type.GetProperty("Right");
			var eitherIndex = member.Type.GetProperty("Index");

			var newExpr = Expression.Block(
				ReadOne(stream, member, member.Type, null),
				Expression.IfThen(Expression.Equal(Expression.Property(member, eitherIndex), Expression.Constant((byte)1)),
					ReadMaybeType(Expression.Property(member, eitherLeft), stream, null)
				),
				Expression.IfThen(Expression.Equal(Expression.Property(member, eitherIndex), Expression.Constant((byte)2)),
					ReadMaybeType(Expression.Property(member, eitherRight), stream, null)
				)
			);

			return newExpr;
		}

		static BlockExpression ReadAnyType(MemberExpression member, Expression stream, StarSerializeAttribute attrib)
		{
			var any = Expression.Variable(member.Type, "type");
			var anyValue = member.Type.GetProperty("Value");
			var anyIndex = member.Type.GetProperty("Index");

			var types = member.Type.GetGenericArguments();
			var gTypes = Expression.Constant(types, typeof(Type[]));

			var indexExpr = Expression.Variable(typeof(int), "idx");
			var type = Expression.Variable(typeof(Type), "type");

			var exprs = new List<Expression>();
			foreach (Type t in types)
			{
				var newExpr = Expression.Block(new[] { any, indexExpr, type },
					Expression.Assign(any, member),
					Expression.IfThen(
						Expression.Equal(any, Expression.Constant(null)),
						Expression.Assign(any, ReadOne(stream, member, member.Type, attrib))
					),
					Expression.Assign(indexExpr, Expression.Decrement(Expression.Convert(Expression.Property(member, anyIndex), typeof(int)))),
					Expression.IfThen(Expression.Not(Expression.Or(
							Expression.LessThan(indexExpr, Expression.Constant(0)),
							Expression.GreaterThanOrEqual(indexExpr, Expression.ArrayLength(gTypes))
						)),
					Expression.Block(
						Expression.Assign(type, Expression.ArrayAccess(gTypes, indexExpr)),
						Expression.IfThen(
							Expression.Equal(type, Expression.Constant(t)),
								ReadOne(stream, Expression.Property(any, anyValue), t, null)
							)
						)
					)
				);

				exprs.Add(newExpr);
			}

			return Expression.Block(exprs);
		}

		static Expression WriteOne(Expression stream, MemberExpression source, Type sourceType, StarSerializeAttribute serializeAttrib)
		{
			try
			{
				var type = GetCollectionTypeOrSelf(sourceType);
				var isComplex = IsComplex(type);
				var func = isComplex
					? PacketSerializers.ContainsKey(type)
					? PacketSerializers[type].Item1
					: BuildSerializer(type)
					: GetSerializableType(type).Serializer;
				var funcType = GetFuncType(func); //We need to downcast the member because of contravariance with enums or Complex type boxing.

				if (IsList(sourceType))
				{
					return GetListWriter(stream, source, type, func, serializeAttrib);
				}

				var writer = Expression.Invoke(Expression.Constant(func), stream, Expression.Convert(source, funcType)) as Expression;

				return writer;
			}
			catch (Exception ex)
			{
				ex.LogError();
				throw;
			}
		}

		static Expression ReadOne(Expression stream, MemberExpression dest, Type destType, StarSerializeAttribute serializeAttrib)
		{
			//If we're dealing with a collection, take the generic parameter type.
			var type = GetCollectionTypeOrSelf(destType);
			//If the type is complex, deserialize recursively, else read primitives.
			var func = IsComplex(type)
				? PacketSerializers.ContainsKey(type)
				? PacketSerializers[type].Item2
				: BuildDeserializer(type)
				: GetSerializableType(type).Deserializer;

			var reader = Expression.Convert(Expression.Invoke(Expression.Constant(func), stream), type) as Expression;

			if (IsList(destType))
			{
				return Expression.Assign(dest, GetListReader(stream, dest, reader, type, serializeAttrib));
			}

			return Expression.Assign(dest, reader);
		}

		static BlockExpression GetListWriter(Expression stream, MemberExpression member, Type genericType, Delegate func, StarSerializeAttribute serializeAttrib)
		{
			var counter = Expression.Variable(typeof(int), "counter");
			var exit = Expression.Label();
			var listLength = GetListCount(member, genericType);
			var currentItem = Expression.Property(member, "Item", counter);
			var block = Expression.Block(new[] { counter },
				WriteLengthIfNotGreedy(stream, member, listLength, serializeAttrib.Length),
				Expression.Loop(
					Expression.IfThenElse(Expression.LessThan(counter, listLength),
						Expression.Block(
							Expression.Invoke(Expression.Constant(func), stream, currentItem),
							Expression.Assign(counter, Expression.Increment(counter))),
						Expression.Break(exit)),
					exit));
			return block;
		}

		static BlockExpression GetListReader(Expression stream, MemberExpression member, Expression reader, Type genericType, StarSerializeAttribute serializeAttrib)
		{
			var length = Expression.Variable(typeof(int), "length");
			var listType = typeof(List<>).MakeGenericType(genericType);
			var list = Expression.Variable(listType, "list");
			var exit = Expression.Label();
			var block = Expression.Block(new[] { length, list },
				Expression.Assign(list, Expression.New(listType)),
				ReadLengthIfNotGreedy(stream, member, length, serializeAttrib.Length),
				Expression.Loop(
					Expression.IfThenElse(ShouldReadMore(stream, member, GetListCount(list, genericType), length),
						Expression.Call(list, "Add", null, reader),
						Expression.Break(exit)),
					exit),
				list);
			return block;
		}

		#region Utility methods

		static Expression ReadLengthIfNotGreedy(Expression stream, MemberExpression member, Expression length, int serializeLength)
		{
			return serializeLength <= 0 ? IsGreedyList(member)
				? Expression.Empty() as Expression
				: Expression.Assign(length, Expression.Invoke(_collectionLengthReaderExpr, stream))
				: Expression.Assign(length, Expression.Constant(serializeLength));
		}

		static Type GetFuncType(Delegate func)
		{
			return func.Method.GetParameters().Last().ParameterType;
		}

		static MemberExpression GetListCount(Expression member, Type genericType)
		{
			return Expression.Property(member, typeof(ICollection<>).MakeGenericType(genericType), "Count");
		}

		static Expression ShouldReadMore(Expression stream, MemberExpression member, Expression counter, Expression length)
		{
			return IsGreedyList(member)
				? Expression.IsFalse(Expression.Invoke(_isStreamOver, stream))
				: Expression.LessThan(counter, length) as Expression;
		}

		static bool IsGreedyList(MemberExpression member)
		{
			return Attribute.IsDefined(member.Member, typeof(GreedyAttribute));
		}

		static bool IsConditional(MemberExpression member)
		{
			return Attribute.IsDefined(member.Member, typeof(StarSerializeCondition));
		}

		static Expression WriteLengthIfNotGreedy(Expression stream, MemberExpression member, Expression length, int serializeLength)
		{
			return serializeLength <= 0 ? IsGreedyList(member)
			? Expression.Empty()
			: Expression.Invoke(_collectionLengthWriterExpr, stream, length) as Expression
			: Expression.Empty();
		}

		static bool IsList(Type type)
		{
			return type.IsGenericType && typeof(IList<>).IsAssignableFrom(type.GetGenericTypeDefinition());
		}

		static bool IsAnyType(Expression member)
		{
			return typeof(Any).IsAssignableFrom(member.Type);
		}

		static bool IsMaybeType(Expression member)
		{
			return typeof(Maybe).IsAssignableFrom(member.Type);
		}

		static bool IsEitherType(Expression member)
		{
			return typeof(Either).IsAssignableFrom(member.Type);
		}

		static bool IsComplex(Type type)
		{
			return type.IsClass && Serializables.All(p => p.Type != type);
		}

		static IEnumerable<Tuple<PropertyInfo, StarSerializeAttribute>> GetMembers(Type type)
		{
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(GetMember).Where(p => p != null);
		}

		static Tuple<PropertyInfo, StarSerializeAttribute> GetMember(PropertyInfo pInfo)
		{
			StarSerializeAttribute soa = pInfo.GetCustomAttribute<StarSerializeAttribute>();

			if (soa == null)
				return null;

			return Tuple.Create(pInfo, soa);
		}

		static Type GetSelfOfUnderlying(Type type)
		{
			return type.IsEnum ? Enum.GetUnderlyingType(type) : type;
		}

		static SerializableType GetSerializableType(Type type)
		{
			return _serializables[type.IsGenericType ? type.GetGenericTypeDefinition() : GetSelfOfUnderlying(type)];
		}

		static Type GetCollectionTypeOrSelf(Type type)
		{
			if (IsList(type))
			{
				return type.GetGenericArguments().First();
			}

			return type;
		}

		#endregion

	}
}
