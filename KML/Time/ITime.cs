using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.KML.Time
{
    /// <summary>
    /// 時制を表すインターフェイス
    /// </summary>
    public interface ITime
    {
        /// <summary>
        /// 時刻情報をKMLのフォーマットで返します
        /// </summary>
        /// <returns>文字列化したタイム</returns>
        string GetKmlTime();
    }
}
