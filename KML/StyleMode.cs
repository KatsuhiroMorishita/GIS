using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.KML
{
    /// <summary>
    /// スタイルの出力形式
    /// <para>内部に格納されたオブジェクト個々にスタイルを適用するかどうかを決定します。</para>
    /// </summary>
    public enum StyleMode
    {
        /// <summary>
        /// 共通のスタイル
        /// </summary>
        Common,
        /// <summary>
        /// 独自のスタイル
        /// </summary>
        Unique
    }
}
