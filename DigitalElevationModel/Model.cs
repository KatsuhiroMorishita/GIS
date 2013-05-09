/****************************************************************************
 * Model.cs
 * DEMマップのモデル（種別）を表す列挙体
 * 
 * [history]
 *      2012/5/22   開発開始
 * **************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.DigitalElevationModel
{
    /// <summary>
    /// DEMマップのモデル（種別）を表す列挙体
    /// </summary>
    public enum Model
    { 
        /// <summary>
        /// 不明なモデル
        /// </summary>
        Unknown,
        /// <summary>
        /// 日本の国土地理院が整備しているDEMのGML形式
        /// </summary>
        JPGIS2x_GML,
        /// <summary>
        /// 日本の国土地理院が整備しているDEM
        /// </summary>
        JPGIS2x
    }
}
