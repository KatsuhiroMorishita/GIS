/*****************************************************
 * MapLocalFeature.cs
 * マップの特徴量を計算or返すクラス
 * 
 * 
 * 　開発者：K.Morishita Kumamoto-University Japan
 * 　動作環境：.NET Framework 4
 * 　開発履歴：
 * 　        2011/7/2    NA値の追加
 * 　                    相対座標から緯度・経度に直すなどのメソッドを整備
 * 　        2011/7/6    Position2MeshCorner等関連メソッドを定義
 * 　                    DEMデータでは標高0m以下を-9999.0で表すので、NA値を-99999.0に変更した。
 * 　        2011/9/16   Initialize()にデフォルト値を設定した
 * 　        2011/10/2   メモ：将来、+演算子に対応させて重ね合わせをやってみたいと思う。
 * 　                    今後の用途によるが、nameや属性・単位等がメンバにあっても良いと思う。
 * 　        2011/10/3   メモ：マップ同士の重ね合わせがあるといいなぁと思う。
 * 　        2011/11/20  LocalData構造体にToString()を追加
 * 　        2011/12/15  MapBase内のメッシュサイズ計算における引数の渡す順番ミスを修正
 * 　                    Pos構造体にCompareTo()を追加した。Array.Sort()で配列をソートできるようになった。
 * 　                    MapLocalFeatureクラスを新設
 * 　                    GetMapBetween(),GetMapHiger(),GetMapLower(),GetMapMatch(),GetMatchPosArray(),SaveAsImage()を整備。正常な動作を確認した。
 * 　        2011/12/16  コメントの見直しと関数の整理などを実施した。
 * 　                    マップ内のローカルな座標系の配列を緯度・経度の配列に変換するメソッドToBLHarray()を実装した。
 * 　        2011/12/18  MapCorner構造体を、コンストラクタ生成時だけでなく要素にアクセスする際にも引数をチェックするようにプロパティを新設
 * 　                    今までpublicにしていた座標値はprivateにして隠ぺいした。
 * 　                    おかげで他のソースコードでの変更点が多数あったが、エラー原因の追及時間が短くなると思う。
 * 　                    あと、MapCorner構造体にToString()を追加した。
 * 　                    
 * 　                    GetValue(Pos _pos)を整備し、GetValue(BLH _position)の構造をそれに併せて変更した。
 * 　                    GetPosDiff()をGetDifferentialPosition()に名称変更
 * 　                    Pos[]配列によるAddValue()を新設
 * 　                    PosのUseful()メソッドを新設
 * 　        2012/4/30   PosへEquals()を追加
 * 　                    MapBase.csより分離
 * 　                    MapBaseクラス内にこの特徴量を計算するメソッドを入れなかった理由ってなんだっけ？
 * 　        2012/7/27   GetFeature(BLH pos)を整備
 * 　        2012/8/30   GetSlope(int x, int y)を整備
 * 　        2012/9/4    GetSlope()のデバッグ
 * 　        2012/9/30   マップクラスをジェネリックにしたのに対応した。
 * 　        2012/10/1   GetFeature(Pos pos)を追加した。
 * 　        2012/10/3   GetSlope()の仕様を変更して、未定義領域ではdouble.NaNを格納して返すように変更した。
 * ***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GNSS;

namespace GIS.MapBase
{
    /// <summary>
    /// MapScalarクラスの特徴量を計算するクラス
    /// <para>ジェネリックの型は、int, float, double等で利用可能です（構造体の利用は不可）。</para>
    /// </summary>
    public partial class MapLocalFeature<T>
        where T: IComparable
    {
        /*　メンバ変数　***************************************************************/
        /// <summary>マップ</summary>
        private MapScalar<T> map;
        /*　プロパティ　***************************************************************/
        /*　メンバ関数（メソッド）*******************************************+*********/
        /// <summary>
        /// 指定座標(Pos形式)の局所的な特徴を返す
        /// <para>指定座標がマップの座標範囲内であれば必ず特徴データを返します。</para>
        /// </summary>
        /// <param name="x">指定座標x</param>
        /// <param name="y">指定座標y</param>
        /// <returns>特徴データ（構造体）</returns>
        /// <exception cref="SystemException">マップの範囲外を指定した場合にスロー</exception>
        public Feature GetFeature(int x, int y)
        {
            double center, upper, lower, right, left;
            double max = double.MinValue, min = double.MaxValue;
            double[] value = new double[5];

            if (x >= 0 && y >= 0 && x < this.map.Size.x && y < this.map.Size.y)
            {
                center = Convert.ToDouble(this.map[x, y]);
                if (y != 0) upper = Convert.ToDouble(this.map[x, y - 1]); else upper = center;
                if (y != this.map.Size.y - 1) lower = Convert.ToDouble(this.map[x, y + 1]); else lower = center;
                if (x != 0) left = Convert.ToDouble(this.map[x - 1, y]); else left = center;
                if (x != this.map.Size.x - 1) right = Convert.ToDouble(this.map[x + 1, y]); else right = center;
                value[0] = center; value[1] = upper; value[2] = lower; value[3] = right; value[4] = left;
                for (int i = 0; value.Length > i; i++) if (value[i] > max) max = value[i];  // 周囲を含めた最大値を探す
                for (int i = 0; value.Length > i; i++) if (value[i] < min) min = value[i];  // 周囲を含めた最小値を探す
                max = (max - center) / 2 + center;                                          // 中央であり得る最高を計算
                min = (min - center) / 2 + center;                                          // 中央であり得る最低を計算
            }
            else
            {
                throw new SystemException("引数が範囲内にありません。");
            }
            return new Feature(center, max, min);
        }
        /// <summary>
        /// 指定座標(Pos形式)の局所的な特徴を返す
        /// </summary>
        /// <param name="pos">指定座標</param>
        /// <returns>特徴データ</returns>
        public Feature GetFeature(Pos pos)
        {
            return this.GetFeature(pos.x, pos.y);
        }
        /// <summary>
        /// 指定座標(Blh形式)の局所的な特徴を返す
        /// <para>指定座標がマップの座標範囲内であれば必ず特徴データを返します。</para>
        /// <para>マップの範囲外を指定した場合はエラーがスローされます。</para>
        /// </summary>
        /// <param name="pos">指定座標</param>
        /// <returns>特徴データ（構造体）</returns>
        public Feature GetFeature(Blh pos)
        {
            Pos _pos = this.map.Position2XYaddress(pos);
            return this.GetFeature(_pos.x, _pos.y);
        }
        /// <summary>
        /// 平均最大法を用いて傾斜角を求め、その最大傾斜方位と共にタプルで返します
        /// <para>返り値であるタプルへの格納順は、傾斜角・方位の順です。</para>
        /// <para>マップ境界においてはdouble.NaNを格納して返します。</para>
        /// <para>
        /// 方位は、ラスタ値が高い方を表し、北を0°とし時計回りに+を取ります。
        /// 例えば、方位90°ならば、東側に向かって
        /// </para>
        /// <para>傾斜角の演算アルゴリズムはArcGISに準じる。</para>
        /// <para>http://help.arcgis.com/ja/arcgisdesktop/10.0/help/index.html#//009z000000vz000000</para>
        /// <para>Burrough, P. A., and McDonell, R. A., 1998. Principles of Geographical Information Systems (Oxford University Press, New York), 190 pp.</para>
        /// </summary>
        /// <param name="x">指定座標x</param>
        /// <param name="y">指定座標y</param>
        /// <returns>傾斜角</returns>
        /// <exception cref="SystemException">マップの範囲外を指定した場合にスロー</exception>
        public Tuple<double, double> GetSlope(int x, int y)
        {
            if (x > 0 && y > 0 && x < this.map.Size.x - 1 && y < this.map.Size.y - 1)
            {
                double a, b, c, d, f, g, h, i;

                a = Convert.ToDouble(this.map[x - 1, y - 1]);
                b = Convert.ToDouble(this.map[x, y - 1]);
                c = Convert.ToDouble(this.map[x + 1, y - 1]);
                d = Convert.ToDouble(this.map[x - 1, y]);
                f = Convert.ToDouble(this.map[x + 1, y]);
                g = Convert.ToDouble(this.map[x - 1, y + 1]);
                h = Convert.ToDouble(this.map[x, y + 1]);
                i = Convert.ToDouble(this.map[x + 1, y + 1]);

                var unit = this.map.MeshSize;
                double dzdx = ((c + 2 * f + i) - (a + 2 * d + g)) / (8 * unit.E);
                double dzdy = ((g + 2 * h + i) - (a + 2 * b + c)) / (8 * unit.N);
                var riseRun = Math.Sqrt(dzdx * dzdx + dzdy * dzdy);                 // 傾斜を計算
                var slopeDegree = Math.Atan(riseRun) * 57.29578;                    // 単位変換　180 / Pi = 57.29578・・・
                var direction = Math.Atan2(dzdy, dzdx) + 90.0;                      // 方位計算
                if (direction < 0.0) direction += 360.0;
                return Tuple.Create(slopeDegree, direction);
            }
            else if (x < 0 || y < 0 && x >= this.map.Size.x || y >= this.map.Size.y)
            {
                throw new SystemException("MapLocalFeatureクラスのGetSlope()にてエラーがスローされました。引数が範囲内にありません。");
            }
            else
                return Tuple.Create(double.NaN, double.NaN);
        }
        /// <summary>
        /// コンストラクタ
        /// <para>MapBase型のマップを渡して下さい。</para>
        /// <para>渡されたマップは内部で参照されます。</para>
        /// </summary>
        /// <param name="_map">マップ</param>
        /// <exception cref="SystemException">インスタンスを確保済みのMapをセットしなかった場合にスロー</exception>
        public MapLocalFeature(MapScalar<T> _map)
        {
            if (_map == null) throw new SystemException("インスタンスを確保済みのMapをセットして下さい。");
            this.map = _map;
        }
    }
    public partial class MapLocalFeature<T>
    {
        /// <summary>
        /// 局所的な特徴を表す構造体
        /// </summary>
        public struct Feature
        {
            /*　メンバ変数　***************************************************************/
            /// <summary>指定メッシュの値</summary>
            private double centerValue;
            /// <summary>見込まれる最高値</summary>
            private double max;
            /// <summary>見込まれる最低値</summary>
            private double min;
            /*　プロパティ　***************************************************************/
            /// <summary>
            /// 指定メッシュの値
            /// </summary>
            public double Center { get { return this.centerValue; } }
            /// <summary>
            /// 指定メッシュがとり得る周囲から勘案した最高値
            /// </summary>
            public double Max { get { return this.max; } }
            /// <summary>
            /// 指定メッシュがとり得る周囲から勘案した最低値
            /// </summary>
            public double Min { get { return this.min; } }
            /*　メンバ関数（メソッド）***************************************************/
            /// <summary>
            /// 指定値が範囲内にあるのかを返す
            /// </summary>
            /// <param name="value">閾値</param>
            /// <returns>true：範囲内にある</returns>
            public Boolean CheckBetween(double value)
            {
                if (this.max >= value && this.min <= value)
                    return true;
                else
                    return false;
            }
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="_center">注目メッシュの持つ値</param>
            /// <param name="_max">注目メッシュが取り得る最高値</param>
            /// <param name="_min">注目メッシュが取り得る最低値</param>
            public Feature(double _center, double _max, double _min)
            {
                this.centerValue = _center;
                this.max = _max;
                this.min = _min;
            }
        }
    }
}
