using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ItoKonnyaku.Commons.Extensions
{
    /// <summary>
    /// Directory クラスに関する汎用関数を管理するクラス
    /// </summary>
    public static class DirectoryUtils
    {
        /// <summary>
        /// 指定したパスにディレクトリが存在しない場合
        /// すべてのディレクトリとサブディレクトリを作成します
        /// (指定されたパスの末尾がファイル名のように見える場合はパスまで作成）
        /// </summary>
        public static DirectoryInfo SafeCreateDirectory(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (Directory.Exists(dir))
            {
                return null;
            }
            return Directory.CreateDirectory(dir);
        }
    }
}
