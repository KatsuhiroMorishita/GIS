/******************************************************
 * Icon.cs
 * KMLのPoint用のアイコンの種類を表します。
 * 
 * [更新履歴]
 *      2012/6/2    開発開始
 *      2012/6/9    ToString()でメンバ名を取得できたので、クラスをやめて列挙体へ変更した。
 * ****************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.KML.Point
{
    /// <summary>
    /// Pointのアイコンを表す列挙体
    /// </summary>
    public enum Icon
    {
        /// <summary>
        /// 中心にドットのある円
        /// </summary>
        placemark_circle,
        /// <summary>
        /// 円
        /// </summary>
        shaded_dot
    }
}
