using System;
using System.ComponentModel;
using System.Data;

namespace ItoKonnyaku.Commons.Extensions
{

/// <summary>
/// 数字処理の拡張メソッド
/// </summary>
    public static class NumericUtils
    {
        /// <summary>
        ///     元値が範囲内に収まるか回答する。
        /// </summary>
        /// <param name="org">元値</param>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <returns>min / org / max </returns>
        public static bool IsInRange<T>(this T org, T min, T max) where T : IComparable<T>
        {
            //if (min.CompareTo(max)>0) { return org;  };

            return org.CompareTo(min) < 0 ? false : (org.CompareTo(max) > 0 ? false : true);
        }

        /// <summary>
        ///     元値を範囲内に収まるように回答する。
        /// </summary>
        /// <param name="org">元値</param>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <returns>min / org / max </returns>
        public static T InRange<T>(this T org, T min, T max) where T : IComparable<T>
        {
            //if (min.CompareTo(max)>0) { return org;  };

            return org.CompareTo(min) < 0 ? min : (org.CompareTo(max) > 0 ? max : org);
        }

        /// <summary>
        ///     文字列式を数式解釈した上で計算し返答する。
        /// </summary>
        /// <typeparam name="T">変換クラス(int,double)</typeparam>
        /// <param name="s">文字列(拡張メソッド)</param>
        /// <param name="args">文字列format実体、{0}）</param>
        /// <returns>T:計算結果</returns>
        public static T Calc<T>(this string s, params object[] args)
        {
            using (DataTable dt = new DataTable())
            {
                s = string.Format(s, args);
                object result = dt.Compute(s, "");
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter.ConvertFromString(result.ToString());
            }
        }
    }
}
