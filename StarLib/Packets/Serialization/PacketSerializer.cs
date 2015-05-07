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
using System.Collections;
using System.Collections.Concurrent;
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
            new StarSerializableType<byte[]>((writer, val) => writer.WriteUInt8Array(val), reader => reader.ReadUInt8Array())
        };

        private static readonly Dictionary<Type, SerializableType> _serializables = Serializables.ToDictionary(p => p.Type);

        private static readonly Func<StarReader, int> _collectionLengthReader = reader => (int)reader.ReadVLQ();
        private static readonly Action<StarWriter, int> _collectionLengthWriter = (writer, i) => writer.WriteVlq((ulong)i);

        private static readonly ConstantExpression _isStreamOver = Expression.Constant(new Func<StarReader, bool>(s => s.BaseStream.Length == s.BaseStream.Position));
        private static readonly Expression _collectionLengthReaderExpr = Expression.Constant(_collectionLengthReader);
        private static readonly Expression _collectionLengthWriterExpr = Expression.Constant(_collectionLengthWriter);

        public static readonly ConcurrentDictionary<Type, Action<StarWriter, object>> PacketSerializers;
        public static readonly ConcurrentDictionary<Type, Func<StarReader, object>> PacketDeserializers;

        static PacketSerializer()
        {
            PacketSerializers = new ConcurrentDictionary<Type, Action<StarWriter, object>>();
            PacketDeserializers = new ConcurrentDictionary<Type, Func<StarReader, object>>();
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
            BuildSerializer(type);
            BuildDeserializer(type);
        }

        public static void Serialize(object value, StarWriter writer)
        {
            try
            {
                Type type = value.GetType();

                BuildAndStore(type);

                var lambda = PacketSerializers[type];

                lambda(writer, value);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Serialization error. Type: {0}", value.GetType()), e);
            }
        }

        /// <summary>
        ///     Gets or builds a deserialization expression tree to convert the specified <see cref="StarReader" /> to an instance of
        ///     the specified type.
        /// </summary>
        public static object Deserialize(StarReader reader, Type type)
        {
            object result = DeserializeInternal(reader, type);

            //if (reader.DataLeft != 0)
            //	StarLog.DefaultLogger.Warn("Packet {0} is incomplete ({1} bytes left)!", type.FullName, reader.DataLeft);

            return result;
        }

        private static object DeserializeInternal(StarReader reader, Type type)
        {
            try
            {
                BuildAndStore(type);

                var lambda = PacketDeserializers[type];

                return lambda(reader);
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
            if (PacketSerializers.ContainsKey(arg))
                return PacketSerializers[arg];

            //To store the lambdas in the same dictionary the parameter is boxed and unboxed in runtime.
            var boxedInstance = Expression.Parameter(typeof(object), "boxedInstance");
            var instance = Expression.Variable(arg, "instance");
            var dest = Expression.Parameter(typeof(StarWriter), "dest");
            var block = Serialize(arg, dest, instance, boxedInstance);
            var lambda = Expression.Lambda<Action<StarWriter, object>>(block, dest, boxedInstance);
            var action = lambda.Compile();

            if (!PacketSerializers.ContainsKey(arg))
                PacketSerializers.GetOrAdd(arg, action);

            return action;
        }

        static Func<StarReader, object> BuildDeserializer(Type arg)
        {
            if (PacketDeserializers.ContainsKey(arg))
                return PacketDeserializers[arg];

            var instance = Expression.Variable(arg, "instance");
            var source = Expression.Parameter(typeof(StarReader), "source");
            var block = Deserialize(instance, source);
            var lambda = Expression.Lambda<Func<StarReader, object>>(block, source);
            var func = lambda.Compile();

            if (!PacketDeserializers.ContainsKey(arg))
                PacketDeserializers.GetOrAdd(arg, func);

            return func;
        }

        static BlockExpression Serialize(Type arg, Expression dest,
            ParameterExpression instance, Expression boxedInstance)
        {
            var write = WriteToBuffer(instance, arg, dest);

            var block = Expression.Block(new[] { instance },
                Expression.Assign(instance, Expression.Convert(boxedInstance, arg)),
                write,
                dest);

            return block;
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
            var members = GetMembers(type).ToList();

            var expressions = new List<Expression>();

            if (IsMaybeType(instance))
                expressions.Add(WriteMaybeType(instance, dest));

            bool skipOne = false;
            for (int i = 0; i < members.Count; i++)
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
                    var serialize = typeof(PacketSerializer).GetMethod("Serialize", new[] { typeof(object), typeof(StarWriter) });

                    var newExpr = Expression.Block(new[] { write },
                            Expression.Invoke(Expression.Constant(_serializables[typeof(bool)].Serializer), dest, member),
                            //WriteOne(dest, member, type, members[i].Item2),
                            Expression.Assign(write, member),
                            Expression.IfThen(Expression.Equal(write, Expression.Constant(true)),
                                Expression.Call(serialize, Expression.Property(instance, members[i + 1].Item1), dest)
                            //WriteOne(dest, Expression.Property(instance, members[i + 1].Item1), type, members[i + 1].Item2)
                            )
                        );

                    skipOne = true;

                    expressions.Add(newExpr);
                }
                else if (IsAnyType(member))
                {
                    var write = WriteAnyType(member, dest);

                    expressions.Add(write);
                }
                else if (IsMaybeType(member))
                {
                    expressions.Add(WriteMaybeType(member, dest));
                }
                //else if (IsEitherType(member))
                //{
                //	expressions.Add(WriteEitherType(member, dest));
                //}
                else
                {
                    var write = WriteOne(dest, member, member.Type, members[i].Item2);

                    expressions.Add(write);
                }

            }

            return expressions.Any() ? Expression.Block(expressions) : Expression.Block(Expression.Empty());
        }

        //TODO: FIX ME
        //static BlockExpression WriteEitherType(MemberExpression member, Expression dest)
        //{
        //	var eitherIndex = member.Type.GetProperty("Index");
        //	var eitherLeft = member.Type.GetProperty("Left");
        //	var eitherRight = member.Type.GetProperty("Right");

        //	var eitherTypes = member.Type.GetGenericArguments();
        //	var leftType = eitherTypes[0];
        //	var rightType = eitherTypes[1];

        //	var leftMaybeType = typeof(Maybe<>).MakeGenericType(leftType);
        //	var rightMaybeType = typeof(Maybe<>).MakeGenericType(rightType);

        //	var left = Expression.Variable(leftMaybeType, "left");
        //	var right = Expression.Variable(rightMaybeType, "right");

        //	var maybeLeftVal = leftMaybeType.GetProperty("Value");
        //	var maybeRightVal = rightMaybeType.GetProperty("Value");

        //	var newExpr = Expression.Block(new[] { left, right },
        //		Expression.Assign(left, Expression.Property(member, eitherLeft)),
        //		Expression.Assign(right, Expression.Property(member, eitherRight)),
        //		Expression.IfThenElse(
        //			Expression.NotEqual(Expression.Property(left, maybeLeftVal), Expression.Constant(null)),
        //			Expression.Block(
        //				Expression.Assign(Expression.Property(member, eitherIndex), Expression.Constant((byte)1)),
        //				WriteOne(dest, Expression.Property(member, eitherIndex), typeof(byte), null),
        //				WriteMaybeType(Expression.Property(member, eitherLeft), dest)
        //			),
        //			Expression.IfThenElse(
        //				Expression.NotEqual(Expression.Property(right, maybeRightVal), Expression.Constant(null)),
        //				Expression.Block(
        //					Expression.Assign(Expression.Property(member, eitherIndex), Expression.Constant((byte)2)),
        //					WriteOne(dest, Expression.Property(member, eitherIndex), typeof(byte), null),
        //					WriteMaybeType(Expression.Property(member, eitherRight), dest)
        //				),
        //				Expression.Block(
        //					Expression.Assign(Expression.Property(member, eitherIndex), Expression.Constant((byte)0)),
        //					WriteOne(dest, Expression.Property(member, eitherIndex), typeof(byte), null)
        //				)
        //			)
        //		)
        //	);

        //	return newExpr;
        //}

        static BlockExpression WriteMaybeType(Expression member, Expression dest)
        {
            var maybeValueProp = member.Type.GetProperty("Value");
            var valueType = member.Type.GetGenericArguments().First();
            var serializer = typeof(PacketSerializer).GetMethod("Serialize", new[] { typeof(object), typeof(StarWriter) });

            var newExpr = Expression.Block(
                Expression.IfThenElse(Expression.NotEqual(Expression.Property(member, maybeValueProp), Expression.Constant(null)),
                Expression.Block(
                        Expression.Invoke(Expression.Constant(_serializables[typeof(byte)].Serializer), dest, Expression.Constant((byte)1)),
                        WriteOne(dest, Expression.Property(member, maybeValueProp), valueType, null)
                    //Expression.Call(serializer, Expression.Property(member, maybeValueProp), dest)
                    ),
                    Expression.Invoke(Expression.Constant(_serializables[typeof(byte)].Serializer), dest, Expression.Constant((byte)0))
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

            var anyIndex = Expression.Property(member, anyIndexProp);
            var valProp = Expression.Property(member, anyValueProp);

            var ctr = Expression.Variable(typeof(int), "ctr");

            Type[] args = member.Type.GetGenericArguments();

            var gTypes = Expression.Constant(args);

            var serializer = typeof(PacketSerializer).GetMethod("Serialize", new[] { typeof(object), typeof(StarWriter) });

            var newExpr = Expression.Block(new[] { ctr, typeExpr },
                Expression.IfThenElse(Expression.NotEqual(valProp, Expression.Constant(null)),
                    Expression.Block(
                        Expression.Assign(typeExpr, Expression.Call(valProp, valType)),
                        Expression.Loop(Expression.Block(
                            Expression.IfThenElse(Expression.AndAlso(
                                Expression.LessThan(ctr, Expression.ArrayLength(gTypes)),
                                Expression.Equal(Expression.ArrayIndex(gTypes, ctr), typeExpr)),
                            Expression.Block(
                                Expression.Assign(anyIndex, Expression.Convert(Expression.Increment(ctr), typeof(byte))),
                                Expression.Break(exit)
                                ),
                                Expression.PostIncrementAssign(ctr)
                            )
                        ), exit),
                        WriteOne(dest, anyIndex, typeof(byte), null),
                        Expression.Call(serializer, valProp, dest)
                    ),
                    Expression.Invoke(Expression.Constant(GetSerializableType(typeof(byte)).Serializer), dest, Expression.Constant((byte)0))
                )
            );

            return newExpr;
        }

        static BlockExpression ReadFromBuffer(Expression instance, Expression stream)
        {
            var members = GetMembers(instance.Type).ToList();
            var expressions = new List<Expression>();

            if (IsMaybeType(instance))
                expressions.Add(ReadMaybeType(instance, stream, null));

            bool skipOne = false;
            for (int i = 0; i < members.Count; i++)
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
                    Expression.Assign(read, Expression.Invoke(Expression.Constant(_serializables[typeof(bool)].Deserializer), stream)),
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
                else if (IsMaybeType(member))
                {
                    expressions.Add(ReadMaybeType(member, stream, members[i].Item2));
                }
                //else if (IsEitherType(member))
                //{
                //	expressions.Add(ReadEitherType(member, stream, members[i].Item2));
                //}
                else
                {
                    expressions.Add(ReadOne(stream, member, member.Type, members[i].Item2));
                }
            }

            return expressions.Any() ? Expression.Block(expressions) : Expression.Block(Expression.Empty());
        }

        static BlockExpression ReadMaybeType(Expression member, Expression stream, StarSerializeAttribute attrib)
        {
            var maybeType = member.Type.GetGenericArguments().First();

            var hasAny = Expression.Variable(typeof(byte), "hasAny");
            var valueProp = member.Type.GetProperty("Value");

            var newExpr = Expression.Block(new[] { hasAny },
                Expression.Assign(member, Expression.New(member.Type)),
                Expression.Assign(hasAny, Expression.Invoke(Expression.Constant(_serializables[typeof(byte)].Deserializer), stream)),
                Expression.IfThen(Expression.Equal(hasAny, Expression.Constant((byte)1)),
                    ReadOne(stream, Expression.Property(member, valueProp), maybeType, null)
                ),
                member
            );

            return newExpr;
        }

        //TODO: FIX ME
        //static BlockExpression ReadEitherType(MemberExpression member, Expression stream, StarSerializeAttribute attrib)
        //{
        //	Type[] eitherTypes = member.Type.GetGenericArguments();

        //	Type leftType = eitherTypes.First();
        //	Type maybeLeftType = typeof(Maybe<>).MakeGenericType(leftType);

        //	var maybeLeft = Expression.Variable(maybeLeftType, "maybe");
        //	var maybeVal = maybeLeftType.GetProperty("Value");
        //	var eitherLeft = member.Type.GetProperty("Left");
        //	var eitherRight = member.Type.GetProperty("Right");

        //	var newExpr = Expression.Block(new[] { maybeLeft },
        //		ReadMaybeType(Expression.Property(member, eitherLeft), stream, null),
        //			//Expression.IfThen(Expression.Equal(Expression.Property(maybeLeft, maybeVal), Expression.Constant(null)),
        //			ReadMaybeType(Expression.Property(member, eitherRight), stream, null)
        //	//)
        //	);

        //	return newExpr;
        //}

        static BlockExpression ReadAnyType(MemberExpression member, Expression stream, StarSerializeAttribute attrib)
        {
            var index = Expression.Property(member, "Index");
            var value = Expression.Property(member, "Value");
            var types = Expression.Constant(member.Type.GetGenericArguments());
            var deserializeMethod = typeof(PacketSerializer).GetMethod("Deserialize", new[] { typeof(StarReader), typeof(Type) });
            var indexAsInt = Expression.Decrement(Expression.Convert(index, typeof(int)));
            var actualType = Expression.ArrayIndex(types, indexAsInt);
            var body = Expression.Block(
                Expression.Assign(member, Expression.New(member.Type)),
                ReadOne(stream, index, typeof(byte), attrib),
                Expression.IfThen(Expression.Not(Expression.Or(
                    Expression.LessThan(indexAsInt, Expression.Constant(0)),
                    Expression.GreaterThanOrEqual(indexAsInt, Expression.ArrayLength(types))
                )),
                Expression.Assign(value, Expression.Call(deserializeMethod, stream, actualType))));

            return body;
        }

        static Expression WriteOne(Expression stream, MemberExpression source, Type sourceType, StarSerializeAttribute serializeAttrib)
        {
            try
            {
                var type = GetCollectionTypeOrSelf(sourceType);
                var isComplex = IsComplex(type);
                var func = isComplex
                    ? PacketSerializers.ContainsKey(type)
                    ? PacketSerializers[type]
                    : BuildSerializer(type)
                    : GetSerializableType(type).Serializer;

                if (IsList(sourceType))
                {
                    var w = GetListWriter(stream, source, type, Expression.Constant(func), serializeAttrib);

                    return w;
                }
                else if (IsDictionary(sourceType))
                {
                    Type[] kvTypes = sourceType.GetGenericArguments();
                    Type valType = kvTypes[1];

                    var valFunc = IsComplex(valType)
                    ? PacketSerializers.ContainsKey(valType)
                    ? PacketSerializers[valType]
                    : BuildSerializer(valType)
                    : GetSerializableType(valType).Serializer;

                    return GetDictionaryWriter(stream, source, type, valType, func, valFunc);
                }

                var funcType = GetFuncType(func); //We need to downcast the member because of contravariance with enums or Complex type boxing.
                var writer = Expression.Invoke(Expression.Constant(func), stream, Expression.Convert(source, funcType));

                return writer;
            }
            catch (Exception ex)
            {
                ex.LogError();
                throw;
            }
        }

        static Expression ReadOne(Expression stream, MemberExpression dest, Type destType, StarSerializeAttribute serializeAttrib, bool toObj = false)
        {
            //If we're dealing with a collection, take the generic parameter type.
            var type = GetCollectionTypeOrSelf(destType);
            //If the type is complex, deserialize recursively, else read primitives.
            var func = IsComplex(type)
                ? PacketDeserializers.ContainsKey(type)
                ? PacketDeserializers[type]
                : BuildDeserializer(type)
                : GetSerializableType(type).Deserializer;

            var reader = Expression.Convert(Expression.Invoke(Expression.Constant(func), stream), type) as Expression;

            if (IsList(destType))
            {
                return Expression.Assign(dest, GetListReader(stream, dest, reader, type, serializeAttrib));
            }
            else if (IsDictionary(destType))
            {
                Type[] kvTypes = destType.GetGenericArguments();
                Type valType = kvTypes[1];

                var valFunc = IsComplex(valType)
                ? PacketDeserializers.ContainsKey(valType)
                ? PacketDeserializers[valType]
                : BuildDeserializer(valType)
                : GetSerializableType(valType).Deserializer;

                var valReader = Expression.Convert(Expression.Invoke(Expression.Constant(valFunc), stream), valType) as Expression;

                return Expression.Assign(dest, GetDictionaryReader(stream, reader, valReader, type, valType));
            }

            return Expression.Assign(dest, toObj ? Expression.Convert(reader, typeof(object)) : reader);
        }

        static BlockExpression GetListWriter(Expression stream, MemberExpression member, Type genericType, Expression func, StarSerializeAttribute serializeAttrib)
        {
            var counter = Expression.Variable(typeof(int), "counter");
            var exit = Expression.Label();
            var listLength = GetListCount(member, genericType);
            var currentItem = Expression.Property(member, "Item", counter);
            var block = Expression.Block(new[] { counter },
                WriteLengthIfNotGreedy(stream, member, listLength, serializeAttrib != null ? serializeAttrib.Length : 0),
                Expression.Loop(
                    Expression.IfThenElse(Expression.LessThan(counter, listLength),
                        Expression.Block(
                            Expression.Invoke(func, stream, currentItem),
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

        static BlockExpression GetDictionaryWriter(Expression stream, MemberExpression member, Type keyType, Type valType, Delegate keyFunc, Delegate valFunc)
        {
            var exit = Expression.Label();

            var count = member.Type.GetProperty("Count");
            var kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valType);
            var kvp = Expression.Variable(kvpType, "kvp");
            var key = kvpType.GetProperty("Key");
            var val = kvpType.GetProperty("Value");
            var enumerable = typeof(IEnumerable).GetMethod("GetEnumerator");
            var enumerator = typeof(IEnumerator);
            var next = enumerator.GetMethod("MoveNext");
            var current = enumerator.GetProperty("Current");

            var ie = Expression.Variable(enumerator, "ie");
            var block = Expression.Block(new[] { kvp, ie },
                Expression.Assign(ie, Expression.Call(member, enumerable)),
                Expression.Invoke(_collectionLengthWriterExpr, stream, Expression.Property(member, count)),
                Expression.Loop(
                    Expression.IfThenElse(Expression.Equal(Expression.Call(ie, next), Expression.Constant(true)),
                        Expression.Block(
                            Expression.Assign(kvp, Expression.Convert(Expression.Property(ie, current), kvpType)),
                            Expression.Invoke(Expression.Constant(keyFunc), stream, Expression.Property(kvp, key)),
                            Expression.Invoke(Expression.Constant(valFunc), stream, Expression.Property(kvp, val))),
                        Expression.Break(exit)),
                    exit));
            return block;
        }

        static BlockExpression GetDictionaryReader(Expression stream, Expression keyReader, Expression valueReader, Type keyType, Type valType)
        {
            var length = Expression.Variable(typeof(int), "length");
            var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
            var dictAdd = dictType.GetMethod("Add", new[] { keyType, valType });

            var dict = Expression.Variable(dictType, "dict");
            var exit = Expression.Label();
            var block = Expression.Block(new[] { length, dict },
                Expression.Assign(dict, Expression.New(dictType)),
                Expression.Assign(length, Expression.Invoke(_collectionLengthReaderExpr, stream)),
                Expression.Loop(
                    Expression.IfThenElse(Expression.LessThan(GetDictionaryCount(dict, keyType, valType), length),
                            Expression.Call(dict, dictAdd, keyReader, valueReader),
                        Expression.Break(exit)),
                    exit),
            dict);

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

        static MemberExpression GetDictionaryCount(Expression member, Type keyType, Type valueType)
        {
            return Expression.Property(member, typeof(Dictionary<,>).MakeGenericType(keyType, valueType), "Count");
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

        static bool IsDictionary(Type type)
        {
            return type.IsGenericType && typeof(Dictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition());
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
            return (type.IsInterface || type.IsClass) && Serializables.All(p => p.Type != type);
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
            if (IsList(type) || IsDictionary(type))
            {
                return type.GetGenericArguments().First();
            }

            return type;
        }

        #endregion

    }
}
