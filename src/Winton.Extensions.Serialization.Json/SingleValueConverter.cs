﻿using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Winton.Extensions.Serialization.Json
{
    public class SingleValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            object value = reader.Value;
            Type underlyingType = Nullable.GetUnderlyingType(objectType);

            if (underlyingType != null)
            {
                if (value == null)
                {
                    return null;
                }

                objectType = underlyingType;
            }

            TypeInfo typeInfo = objectType.GetTypeInfo();

            Type fieldType = GetSingleFieldInfo(typeInfo).FieldType;

            ConstructorInfo constructorInfo =
                typeInfo.DeclaredConstructors.SingleOrDefault(ci => TakesSingleParameterOfType(ci, fieldType));

            if (constructorInfo == null)
            {
                throw new JsonSerializationException(
                    $"{nameof(SingleValueConverter)} can only be used on types with a constructor taking a single parameter of the same type as its backing field.");
            }

            return constructorInfo.Invoke(
                new[]
                {
                    Convert.ChangeType(value, fieldType)
                });
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            writer.WriteValue(GetSingleFieldInfo(value.GetType().GetTypeInfo()).GetValue(value));
        }

        private static FieldInfo GetSingleFieldInfo(TypeInfo typeInfo)
        {
            FieldInfo[] fieldInfos = typeInfo.DeclaredFields.Where(fi => !fi.IsStatic && !fi.IsPublic).ToArray();

            if (fieldInfos.Length != 1)
            {
                throw new JsonSerializationException(
                    $"{nameof(SingleValueConverter)} can only be used on types with a single backing field.");
            }

            return fieldInfos.Single();
        }

        private static bool TakesSingleParameterOfType(MethodBase methodBase, Type type)
        {
            ParameterInfo[] parameterInfos = methodBase.GetParameters();

            return parameterInfos.Length == 1 && parameterInfos.Single().ParameterType == type;
        }
    }
}