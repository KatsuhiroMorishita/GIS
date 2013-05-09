/****************************************************************************
 * ReadMode.cs
 * DEMファイルの読み込みモードを規定する列挙体
 * 
 * [history]
 *      2012/5/23   開発開始
 * **************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.DigitalElevationModel.Reader
{
    /// <summary>
    /// DEMファイルの読み込みモードを規定する列挙体
    /// </summary>
    public enum ReadMode
    {
        /// <summary>
        /// ヘッダー情報のみ
        /// </summary>
        HeaderOnly,
        /// <summary>
        /// ヘッダーとともにDEM値も読み込む
        /// </summary>
        HeaderAndValue
    }
}
