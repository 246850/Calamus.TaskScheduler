using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举 文本值
        /// </summary>
        /// <typeparam name="TEnum">枚举类型 T</typeparam>
        /// <param name="source">当前实例</param>
        /// <returns>文本字符串</returns>
        public static string ToText<TEnum>(this TEnum source) where TEnum : struct
        {
            Type type = typeof(TEnum);
            if (!type.IsEnum)
                throw new ArgumentException("非枚举类型不能调用ToText()方法", source.ToString());

            string name = source.ToString();

            FieldInfo field = type.GetField(name);
            if (field != null)
            {
                object[] customAttributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (customAttributes.Length > 0)
                {
                    string text = ((DescriptionAttribute)customAttributes[0]).Description;
                    return text;
                }
            }

            return string.Empty;
        }
        public static List<SelectListItem> ToSelectList<TEnum>(this TEnum source) 
            where TEnum:struct
        {
            Type type = source.GetType();
            if (!type.IsEnum)
                throw new Exception("非枚举类型不能调用ToSelectList()方法");

            List<SelectListItem> items = new List<SelectListItem>();
            Array values = Enum.GetValues(type);

            foreach (TEnum temp in values)
            {
                string text = string.Empty;
                int value = Convert.ToInt32(temp);

                FieldInfo field = type.GetField(temp.ToString());
                if (field != null)
                {
                    object[] customAttributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (customAttributes.Length > 0)
                    {
                        text = ((DescriptionAttribute)customAttributes[0]).Description;
                    }
                }

                SelectListItem item = new SelectListItem
                {
                    Text = text,
                    Value = value.ToString()
                };
                items.Add(item);
            }

            return items;
        }

        public static List<int> ToValueList<TEnum>(this TEnum source)
        {
            Type type = source.GetType();
            if (!type.IsEnum)
                throw new Exception("非枚举类型不能调用ToTextValueList()方法");
            Array values = Enum.GetValues(type);

            List<int> results = new List<int>();
            foreach (TEnum value in values)
            {
                results.Add(Convert.ToInt32(value));
            }

            return results;
        }
    }
}
