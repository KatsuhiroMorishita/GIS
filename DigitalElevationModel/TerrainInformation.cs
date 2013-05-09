/*******************************************
 * 地形評価用クラス
 *
 * [開発者]
 *  Katsuhiro Morishita @ Kumamoto-University（森下功啓）
 * [概要]
 *  DEMクラスと連動して、地形情報を取得するためのクラスライブラリです。
 * [開発履歴]
 *      2011/10/1   開発開始
 *      2011/10/3   地形の評価までできるようにしたが、地形の評価には単位メッシュ当たりの距離によって変わるので、割と適当な評価方法ではあるし、
 *                  DEMに使用するメッシュサイズが変われば定数を変えねばならないので付け焼刃的だが現時点では問題にならない。
 *      2011/10/7   メッシュのサイズを利用して傾斜を評価する様に更新した。
 *      2011/11/25  TerrainInformation()において、窪地と小ピークの影響を考慮するように変更した。
 *      2011/12/18  MapBaseクラスに対応させた
 *      2012/5/25   DEMクラス群の再編に伴い、DEMクラスとMapクラスの両方に対応していたのをMapのみに一本化した。
 *      2012/6/2    コンストラクタで引数としてインスタンスを確保済みのMapを渡したのにエラーがスローされるバグを修正
 *                  コメントが古いままになっていた部分を修正
 *                  GetGeoFeature()が返値をnull許容としていたが、エラー対策を取ったこととコンストラクタでの引数をインスタンス確保済みのMapに限定したことで必要なくなったため、これをやめた。
 *      2012/7/10   コメントの体裁を調整
 *      2012/7/27   デバッグのためにGetGeoFeature(BLH pos)を整備
 *      2012/8/2    Feature構造体へToString()を実装した。
 *      2012/8/11   ToString()の有効桁数を調整した。
 *                  バグの修正。
 *      2012/9/30   マップクラスをジェネリックにしたのに対応した。
 * *****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GNSS;
using GIS.MapBase;

namespace GIS.DigitalElevationModel
{
    public partial class TerrainInformation
    {
        /// <summary>
        /// 地形の特徴をまとめた構造体
        /// </summary>
        public struct Feature
        {
            /*　列挙体　*******************************************************************/
            /// <summary>
            /// 地形の特徴を表す
            /// </summary>
            public enum GeographicalFeature
            {
                /// <summary>崖</summary>
                Cliff,
                /// <summary>急峻</summary>
                Precipitous,
                /// <summary>なだらか</summary>
                Gentleness,
                /// <summary>平坦</summary>
                Flat,
                /// <summary>不明</summary>
                NA
            }
            /*　メンバ変数　***************************************************************/
            /// <summary>
            /// 高度
            /// </summary>
            private readonly double myHeight;
            /// <summary>
            /// 見込まれる最大の高さ
            /// </summary>
            private readonly double maxHeight;
            /// <summary>
            /// 見込まれる最低の高さ
            /// </summary>
            private readonly double minHeight;
            /// <summary>
            /// 地形情報（ex.平坦）
            /// </summary>
            private readonly GeographicalFeature geoFeature;
            /*　プロパティ　***************************************************************/
            /// <summary>
            /// 高度[m]
            /// </summary>
            public double Height { get { return this.myHeight; } }
            /// <summary>
            /// 周囲の地形から勘案した最高標高[m]
            /// </summary>
            public double MaxHeight { get { return this.maxHeight; } }
            /// <summary>
            /// 周囲の地形から勘案した最低標高[m]
            /// </summary>
            public double MinHeight { get { return this.minHeight; } }
            /// <summary>
            /// 周囲地形（ex. 急峻(Precipitous)）
            /// </summary>
            public GeographicalFeature GeoFeature { get { return this.geoFeature; } }
            /*　メンバ関数（メソッド）　***************************************************/
            /// <summary>
            /// 地形情報を文字列化します
            /// <para>文字列は、高度・最大高・最小高・地形情報です。</para>
            /// <para>デリミタには任意の文字列を利用できます。</para>
            /// </summary>
            /// <param name="delimiter">区切り文字</param>
            /// <returns>文字列化した地形情報</returns>
            public string ToString(string delimiter)
            {
                StringBuilder sb = new StringBuilder(100);                      // 予め大きなメモリ容量を確保しておく
                sb.Append(this.Height.ToString("0.00")).Append(delimiter);
                sb.Append(this.MaxHeight.ToString("0.00")).Append(delimiter);
                sb.Append(this.MinHeight.ToString("0.00")).Append(delimiter);
                sb.Append(this.GeoFeature.ToString());
                return sb.ToString();
            }
            /// <summary>
            /// 地形情報を文字列化します
            /// <para>文字列は、高度・最大高・最小高・地形情報です。</para>
            /// <para>デリミタにはカンマを利用します。</para>
            /// </summary>
            /// <returns>文字列化した地形情報</returns>
            public override string ToString()
            {
                return this.ToString(",");
            }
            /// <summary>
            /// 地形情報をまとめたクラスのコンストラクタ
            /// </summary>
            /// <param name="_myHeight">高度[m]</param>
            /// <param name="_maxHeight">周囲の地形から勘案した最高標高[m]</param>
            /// <param name="_minHeight">周囲の地形から勘案した最低標高[m]</param>
            /// <param name="_geoFeature">周囲地形（ex. 急峻(Precipitous)）</param>
            public Feature(double _myHeight, double _maxHeight, double _minHeight, GeographicalFeature _geoFeature)
            {
                this.myHeight = _myHeight;
                this.maxHeight = _maxHeight;
                this.minHeight = _minHeight;
                this.geoFeature = _geoFeature;
            }
        }
    }
    /// <summary>
    /// 地形情報を格納するためのクラス
    /// </summary>
    /// <remarks>
    ///     唯の高度情報ではなく、DEMと連動した高度な地形情報を扱うためのクラスとして整備する予定
    ///     開空度などを計算できるようにしたら面白いかもね。
    ///     ついでに、継承を使ってGPS用に拡張したって面白い。
    /// </remarks>
    public partial class TerrainInformation
    {
        /*　メンバ変数　***************************************************************/
        /// <summary>
        /// Mapの特徴取得クラスオブジェクト
        /// </summary>
        private MapLocalFeature<float> mapFeature;
        /// <summary>
        /// Map型のDEMマップ
        /// </summary>
        private MapDem map;
        /*　プロパティ　***************************************************************/
        /*　メンバ関数（メソッド）　***************************************************/
        /// <summary>
        /// 傾斜の度合いを返す
        /// </summary>
        /// <param name="rateOfSlope">傾斜の度合い（単位長あたりの高度差[m]）</param>
        /// <returns>傾きの度合いを列挙体で表現したもの</returns>
        public static Feature.GeographicalFeature GetLevelOfSlope(double rateOfSlope)
        {
            if (rateOfSlope > 3.0)
            {
                return Feature.GeographicalFeature.Cliff;                    // 崖
            }
            else if (rateOfSlope > 0.5)
            {
                return Feature.GeographicalFeature.Precipitous;              // 急峻
            }
            else if (rateOfSlope > 0.2)
            {
                return Feature.GeographicalFeature.Gentleness;               // なめらか
            }
            else
            {
                return Feature.GeographicalFeature.Flat;                     // 平坦
            }
        }
        /// <summary>
        /// 地形情報を返す
        /// </summary>
        /// <param name="_pos">マップのアドレス</param>
        /// <returns>特徴情報</returns>
        /// <exception cref="SystemException">マップの範囲外を指定した場合にスロー</exception>
        public Feature GetGeoFeature(Pos _pos)
        {
            if (_pos.x >= 0 && _pos.y >= 0 && _pos.x < this.map.Size.x && _pos.y < this.map.Size.y)
            {
                var ti = this.mapFeature.GetFeature(_pos.x, _pos.y);
                GNSS.Length unit = this.map.MeshSize;                                       // メッシュサイズを取得
                Feature.GeographicalFeature slope = TerrainInformation.GetLevelOfSlope((ti.Max - ti.Min) / unit.N);
                return new Feature(ti.Center, ti.Max, ti.Min, slope);
            }
            else
                throw new SystemException("引数が範囲内にありません。");
        }
        /// <summary>
        /// 地形情報を返す
        /// <para>座標はBlh形式です。</para>
        /// </summary>
        /// <param name="pos">マップのアドレス</param>
        /// <returns>特徴情報</returns>
        /// <exception cref="SystemException">マップの範囲外を指定した場合にスローが返される可能性があります</exception>
        public Feature GetGeoFeature(Blh pos)
        {
            try
            {
                var ti = this.mapFeature.GetFeature(pos);
                GNSS.Length unit = this.map.MeshSize;                                       // メッシュサイズを取得
                Feature.GeographicalFeature slope = TerrainInformation.GetLevelOfSlope((ti.Max - ti.Min) / unit.N);
                return new Feature(ti.Center, ti.Max, ti.Min, slope);
            }
            catch(Exception e)
            { 
                throw e;    // 再スロー
            }             
        }
        /// <summary>
        /// 地形情報クラスのコンストラクタ
        /// <para>Mapクラスによる初期化を行います。</para>
        /// </summary>
        /// <param name="map">DEMマップオブジェクト</param>
        /// <exception cref="SystemException">インスタンスを確保済みのMapをセットしなかった場合にスロー</exception>
        public TerrainInformation(MapDem map)
        {
            if (map == null) throw new SystemException("インスタンスを確保済みのMapをセットして下さい。");
            this.map = map;
            this.mapFeature = new MapLocalFeature<float>(map);
            return;
        }
    }
}
