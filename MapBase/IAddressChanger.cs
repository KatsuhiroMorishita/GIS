/*****************************************************
 * IAddressChanger.cs
 * Mapクラスのアドレス変換機能をカバーするインターフェイスです。
 * アドレス変換をしたいのだけど、Mapオブジェクトをそのまま渡すのは意味的におかしい場合に利用します。
 * 
 * 
 * 　開発者：K.Morishita (Kumamoto-University Japan @2012)
 * 　動作環境：.NET Framework 4
 * 　開発履歴：
 * 　        2012/7/12   新設。
 * ***************************************************/
using System;

using GNSS;

namespace GIS.MapBase
{
    /// <summary>
    /// アドレス変換インターフェイス
    /// </summary>
    public interface IAddressChanger
    {
        /// <summary>
        /// 指定座標の緯度・経度を返す
        /// </summary>
        /// <param name="address">Blh座標</param>
        /// <returns>マップ内座標</returns>
        Blh XYaddress2Position(Pos address);
        /// <summary>
        /// 指定座標のPosアドレスを返す
        /// </summary>
        /// <param name="position">マップ内座標</param>
        /// <returns>Blh座標</returns>
        Pos Position2XYaddress(Blh position);
    }
}
