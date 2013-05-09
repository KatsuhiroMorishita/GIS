/*****************************************************
 * MapGenericCmp.cs
 * ラスタ形式の地図情報を扱うクラスです。
 * ジェネリックを利用していますので、任意の型の値を格納できます。
 * 比較系のメソッドを実装しています。
 * 
 * マップの取り扱い注意
 * 1)本クラスのコンストラクタ生成時には必ずマップの右上と左下の座標（緯度・経度）をセットして下さい。
 * 
 * 　開発者：K.Morishita (Kumamoto-University Japan @2012)
 * 　動作環境：.NET Framework 4
 * 　開発履歴：
 * 　        2012/9/22   (US time) 新規作成
 * 　        2012/9/29   (JST) MapLocalFeatureクラスの対応を残して、整備できた。
 * 　        2012/9/30   MapLocalFeatureクラスを利用するメソッドをMapScalarへ移して本クラスのデバッグは完了した（と思う）。
 * 　        2012/10/3   コメントを訂正
 * ***************************************************/
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using GNSS;
using GNSS.Field;
using Graphic.ColorClass;
using Graphic;
using Geodesy.GeodeticDatum;
 
namespace GIS.MapBase
{
    /// <summary>
    /// ラスタ形式の地図情報を扱うクラス
    /// <para>比較演算子をサポートする任意の型を格納できます。</para>
    /// <para>MapGenericに比べ、比較系のメソッドを実装しています。</para>
    /// <para>
    /// 要素を2次元配列として表現しています。
    /// 配列は[x,y]で宣言しており、画像でたとえると[0,0]が画面の左上、[x_max, y_max]が画面の右下としています。
    /// 画面の上が北で、右が東です。
    /// 東西方向をx、南北方向をyで表現しています。
    /// xは増えるほどより東に位置することを表し、yは増えるほどより南に位置することを表します。
    /// </para>
    /// </summary>
    public class MapComparable<T> : Map<T>
        where T : IComparable
    {
        #region /*　プロパティ　***************************************************************/
        /// <summary>
        /// マップ中の最大値を取得する
        /// <para>全要素を走査しますので値を返すのに時間がかかります。</para>
        /// </summary>
        public T Max 
        {
            get {
                int x_width = this.d_map.GetLength(0);
                int y_width = this.d_map.GetLength(1);
                T max = this.d_map[0, 0];
                

                for (int x = 0; x < x_width; x++)
                    for (int y = 0; y < y_width; y++)
                        if (this.d_map[x, y].CompareTo(max) > 0) max = this.d_map[x, y];
                return max;
            }
        }
        /// <summary>
        /// マップ中の最大値を取得する
        /// <para>全要素を走査しますので値を返すのに時間がかかります。</para>
        /// </summary>
        public T Min
        {
            get
            {
                int x_width = this.d_map.GetLength(0);
                int y_width = this.d_map.GetLength(1);
                T min = this.d_map[0, 0];

                for (int x = 0; x < x_width; x++)
                {
                    for (int y = 0; y < y_width; y++)
                    {
                        if (this.d_map[x, y].CompareTo(min) < 0) min = this.d_map[x, y];
                    }
                }
                return min;
            }
        }
        #endregion
        #region /*　メンバ関数（メソッド）***************************************************/
        /// <summary>
        /// 指定値よりも低い値を持つ地点をマーキングしたマップを返す
        /// <para>返されたマップは、1.0を条件成立とする。条件不成立は0.0とする。</para>
        /// </summary>
        /// <param name="value">閾値</param>
        /// <returns>MapScalarのfloat型マップクラスオブジェクト</returns>
        public MapScalar<float> GetMapLower(T value)    // 出力を画像出力する場合があるので、MapScalar型で返す
        {
            float[,] newMap = new float[this.d_map.GetLength(0), this.d_map.GetLength(1)];
            int x_width = this.Size.x;
            int y_height = this.Size.y;

            Parallel.For(0, x_width, x =>               // 配列に格納
            {
                Parallel.For(0, y_height, y =>
                {
                    if (this.d_map[x, y].CompareTo(value) < 0)
                        newMap[x, y] = (float)1.0;
                    else
                        newMap[x, y] = (float)0.0;
                });
            });

            MapScalar<float> map = new MapScalar<float>(this.Field);
            map.SetMapData(newMap);
            return map;
        }
        /// <summary>
        /// 指定値よりも高い値を持つ地点をマーキングしたマップを返す
        /// <para>返されたマップは、1.0を条件成立とする。条件不成立は0.0とする。</para>
        /// </summary>
        /// <param name="value">閾値</param>
        /// <returns>MapScalarのfloat型マップクラスオブジェクト</returns>
        public MapScalar<float> GetMapHigher(T value)
        {
            float[,] newMap = new float[this.d_map.GetLength(0), this.d_map.GetLength(1)];
            int x_width = this.Size.x;
            int y_height = this.Size.y;

            Parallel.For(0, x_width, x =>               // 配列に格納
            {
                Parallel.For(0, y_height, y =>
                {
                    if (this.d_map[x, y].CompareTo(value) > 0)
                        newMap[x, y] = (float)1.0;
                    else
                        newMap[x, y] = (float)0.0;
                });
            });

            MapScalar<float> map = new MapScalar<float>(this.Field);
            map.SetMapData(newMap);
            return map;
        }
        /// <summary>
        /// 指定値の間にある値を持つ地点をマーキングしたマップを返す
        /// <para>なお、条件には指定値は含まれない。</para>
        /// <para>2つの引数が同値であれば無評価となり、0で初期化されたマップが返ります。</para>
        /// </summary>
        /// <param name="value1">値1</param>
        /// <param name="value2">値2</param>
        /// <returns>MapScalarのfloat型マップクラスオブジェクト</returns>
        public MapScalar<float> GetMapBetween(T value1, T value2)
        {
            float[,] newMap = new float[this.d_map.GetLength(0), this.d_map.GetLength(1)];
            int x_width = this.Size.x;
            int y_height = this.Size.y;

            if (value1.CompareTo(value2) != 0)
            {
                T upper, lower;
                if (value1.CompareTo(value2) > 0)
                {
                    upper = value1;
                    lower = value2;
                }
                else
                {
                    upper = value2;
                    lower = value1;
                }
                Parallel.For(0, x_width, x =>           // 配列に格納
                {
                    Parallel.For(0, y_height, y =>
                    {
                        if (this.d_map[x, y].CompareTo(lower) > 0 && this.d_map[x, y].CompareTo(upper) < 0)
                            newMap[x, y] = (float)1.0;
                        else
                            newMap[x, y] = (float)0.0;
                    });
                });
            }
            MapScalar<float> map = new MapScalar<float>(this.Field);
            map.SetMapData(newMap);
            return map;
        }
        /// <summary>
        /// 指定値に等しい値を持つ地点をマーキングしたマップを返す
        /// <para>指定値と等しい値を持つ地点を1.0としたマップを返します。</para>
        /// <para>走査範囲については、fieldで指定して下さい。</para>
        /// </summary>
        /// <param name="value">閾値</param>
        /// <param name="field">マップ中の走査範囲領域</param>
        /// <returns>MapScalarのfloat型マップクラスオブジェクト</returns>
        public MapScalar<float> GetMapMatchJust(T value, MapField field)
        {
            field = field.And(new MapField(this.Size));                     // 領域を本インスタンスに合わせて調整（領域が縮小することも有り得る）
            float[,] newMap = new float[this.d_map.GetLength(0), this.d_map.GetLength(1)];
            int x_limit = field.LowerRight.x + 1;
            int y_limit = field.LowerRight.y + 1;

            Parallel.For(field.UpperLeft.x, x_limit, x =>                   // 配列に格納
            {
                Parallel.For(field.UpperLeft.y, y_limit, y =>
                {
                    if (this.d_map[x, y].CompareTo(value) == 0)
                        newMap[x, y] = (float)1.0;
                    else
                        newMap[x, y] = (float)0.0;
                });
            });

            MapScalar<float> map = new MapScalar<float>(this.Field);        // マップの座標をセット
            map.SetMapData(newMap);                                         // マップの中身をセット
            return map;
        }
        /// <summary>
        /// 指定値に等しい値を持つ地点をマーキングしたマップを返す
        /// <para>指定値と等しい値を持つ地点を1.0としたマップを返します。</para>
        /// <para>走査範囲はマップ全体となります。</para>
        /// </summary>
        /// <param name="value">閾値</param>
        /// <returns>MapScalarのfloat型マップクラスオブジェクト</returns>
        public MapScalar<float> GetMapMatchJust(T value)
        {
            return this.GetMapMatchJust(value, new MapField(new Pos(), this.Size));
        }
        #endregion
        #region "コンストラクタ関係"
        /// <summary>
        /// マップの領域を指定してインスタンスを生成するコンストラクタ
        /// <para>
        /// 座標計算を特に使用しない場合を想定しています。
        /// マップのサイズはマップデータの代入時に決定されます。
        /// また、この時にマップデータのインスタンスが確保されます。
        /// </para>
        /// </summary>
        /// <param name="field">マップの領域情報</param>
        public MapComparable(RectangleField field)
            : base(field)
        {
            return;
        }
        /// <summary>
        /// 領域の角の座標とマップのサイズ（ピクセル数）を指定することでマップを生成するコンストラクタ
        /// </summary>
        /// <param name="field">マップの領域情報</param>
        /// <param name="_size">縦横のマップサイズ</param>
        public MapComparable(RectangleField field, Pos _size)
            : base(field, _size)
        {
            return;
        }
        /// <summary>
        /// 領域の角の座標とメッシュサイズを指定することでマップを生成するコンストラクタ
        /// <para>
        /// マップの境界値が曖昧だけども任意のマップを生成したいという場合を想定しています。
        /// なお、角の座標値はメッシュサイズに合わせて微調整されます。
        /// マップは0で初期化されます。
        /// </para>
        /// </summary>
        /// <param name="field">作りたいマップの領域情報（緯度・経度）</param>
        /// <param name="_meshSize">メッシュサイズ[m]</param>
        public MapComparable(RectangleField field, Length _meshSize)
            : base(field, _meshSize)
        {
            return;
        }
        /// <summary>
        /// コピーコンストラクタ
        /// <para>パレメータを始め全てのパラメータをコピーしたインスタンスを生成します。</para>
        /// </summary>
        /// <param name="map">コピー元のマップ</param>
        public MapComparable(MapComparable<T> map)
            : base(map)
        {
            return;
        }
        /// <summary>
        /// デスコンストラクタ
        /// </summary>
        ~MapComparable()
        {
            this.d_map = null;
        }
        #endregion
    }
}
