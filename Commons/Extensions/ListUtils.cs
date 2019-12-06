using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ItoKonnyaku.Commons.Extensions
{
    public enum MoveDirection
    {
        Top , Up , Down , Bottom
    }

    /// <summary>
    ///     ObservableCollectionのMoveまわりの挙動で頻出する処理を拡張メソッド化
    /// </summary>
    public static class ListUtil
    {

        /// <summary>
        /// 　　indexが要素に含まれているかチェック
        /// </summary>
        public static bool CheckIndex<T>(this ObservableCollection<T> target, int index) => 
            (index >= 0 && index < target.Count());
        

        /// <summary>
        ///     要素の移動を試みる
        /// </summary>
        /// <returns>失敗：-1、成功：dst</returns>
        public static int TryMove<T>(this ObservableCollection<T> target,int src, int dst)
        {
            //src,dstがindex外ならエラー
            if (!target.CheckIndex(src) || !target.CheckIndex(dst)) return -1;

            target.Move(src, dst);
            return dst;
        }

        /// <summary>
        /// 　　要素の削除を試みる
        /// </summary>
        /// <returns>失敗：-1、成功：削除後一個上のindex</returns>
        public static int TryRemoveAt<T>(this ObservableCollection<T> target, int index)
        {
            if (!target.CheckIndex(index)) return -1;
            target.RemoveAt(index);
            return (index - 1 < 0) ? ((target.Count == 0) ? -1 : 0) : index - 1;
        }

        /// <summary>
        ///     移動位置を指定して移動する
        /// </summary>
        /// <returns>失敗：-1、成功：dst</returns>
        public static int MoveAt<T>(this ObservableCollection<T> target,int src, MoveDirection direction)
        {
            int dst = 0;

            switch (direction)
            {
                case MoveDirection.Top:
                    dst = 0;
                    break;

                case MoveDirection.Up:
                    dst = src - 1;
                    break;

                case MoveDirection.Down:
                    dst = src + 1;
                    break;

                case MoveDirection.Bottom:
                    dst = target.Count()-1;
                    break;
             }

            return TryMove(target, src, dst);
        }
    }

}
