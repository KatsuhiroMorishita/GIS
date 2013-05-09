/*****************************************************
 * MapScalar.cs
 * 標準型を格納するラスタ地図情報を扱うクラス
 * 傾斜角の演算に対応しています。
 * 
 * マップの取り扱い注意
 * 1)本クラスのコンストラクタ生成時には必ずマップの右上と左下の座標（緯度・経度）をセットして下さい。
 * 
 * 　開発者：K.Morishita (Kumamoto-University Japan @2012)
 * 　動作環境：.NET Framework 4
 * 　開発履歴：
 * 　        2012/9/29   新規作成
 * 　        2012/9/30   デバッグ完了（と思う）
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
    /// 標準型を格納するラスタ地図情報を扱うクラス
    /// <para>傾斜角の演算に対応しています。</para>
    /// <para>ジェネリックの型は、int, float, double等で利用可能です（構造体の利用は不可）。</para>
    /// <para>
    /// 要素を2次元配列として表現しています。
    /// 配列は[x,y]で宣言しており、画像でたとえると[0,0]が画面の左上、[x_max, y_max]が画面の右下としています。
    /// 画面の上が北で、右が東です。
    /// 東西方向をx、南北方向をyで表現しています。
    /// xは増えるほどより東に位置することを表し、yは増えるほどより南に位置することを表します。
    /// </para>
    /// </summary>
    public class MapScalar<T> : MapComparable<T>
        where T: IComparable
    {
        /* メンバ変数 ****************************/
        /// <summary>
        /// 画像として保存する時のフォーマット
        /// </summary>
        private ImageFormat imageFormat;
        /* プロパティ ****************************/
        /// <summary>
        /// マップを画像出力するときのフォーマット
        /// </summary>
        public System.Drawing.Imaging.ImageFormat ImageFormat
        {
            get { return this.imageFormat; }
            set { this.imageFormat = value; }
        }
        #region /* メソッド ****************************/
        /// <summary>
        /// 指定値にマッチする地点をマーキングしたマップを返す
        /// <para>指定座標の周囲を考慮して指定値と合致するかをチェックし、合致した地点を1.0としたマップを返します。</para>
        /// <para>走査範囲については、fieldで指定して下さい。</para>
        /// </summary>
        /// <param name="value">閾値</param>
        /// <param name="field">マップ中の走査範囲領域</param>
        /// <returns>MapScalarのfloat型マップクラスオブジェクト</returns>
        public MapScalar<float> GetMapMatch(T value, MapField field)
        {
            field = field.And(new MapField(this.Size));                     // 領域を本インスタンスに合わせて調整（領域が縮小することも有り得る）
            float[,] newMap = new float[this.d_map.GetLength(0), this.d_map.GetLength(1)];
            int x_limit = field.LowerRight.x + 1;
            int y_limit = field.LowerRight.y + 1;
            var feature = new MapLocalFeature<T>(this);                     // 特徴を計算してくれるクラスを宣言

            Parallel.For(field.UpperLeft.x, x_limit, x =>                   // 配列に格納
            {
                Parallel.For(field.UpperLeft.y, y_limit, y =>
                {
                    if (feature.GetFeature((int)x, (int)y).CheckBetween(Convert.ToDouble(value)))
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
        /// 指定値にマッチする地点をマーキングしたマップを返す
        /// <para>指定座標の周囲を考慮して指定値と合致するかをチェックし、合致した地点を1.0としたマップを返します。</para>
        /// <para>走査範囲はマップ全体となります。</para>
        /// </summary>
        /// <param name="value">閾値</param>
        /// <returns>MapScalarのfloat型マップクラスオブジェクト</returns>
        public MapScalar<float> GetMapMatch(T value)
        {
            return this.GetMapMatch(value, new MapField(new Pos(), this.Size));
        }
        /// <summary>
        /// 傾斜角のマップを返す
        /// </summary>
        /// <returns>生成したマップ</returns>
        public MapScalar<double> GetSlopeMap()
        {
            double[,] newMap = new double[this.d_map.GetLength(0), this.d_map.GetLength(1)];
            int x_limit = this.Size.x;
            int y_limit = this.Size.y;
            var feature = new MapLocalFeature<T>(this);                 // 特徴を計算してくれるクラスを宣言

            Parallel.For(0, x_limit, x =>                               // 配列に格納
            {
                Parallel.For(0, y_limit, y =>
                {
                    var slopeTuple = feature.GetSlope(x, y);
                    if (slopeTuple != null)
                        newMap[x, y] = (float)(slopeTuple.Item1);
                    else
                        newMap[x, y] = float.NaN;
                });
            });

            MapScalar<double> map = new MapScalar<double>(this.Field);  // マップの座標をセット
            map.SetMapData(newMap);                                     // マップの中身をセット
            return map;
        }
        /// <summary>
        /// 指定傾斜角範囲にある地点に1をセットしたマップを返す
        /// </summary>
        /// <param name="maxSlope">最大傾斜角[deg]</param>
        /// <param name="minSlope">最小傾斜角[deg]</param>
        /// <returns>MapScalarのfloat型マップクラスオブジェクト</returns>
        public MapScalar<float> GetMapSlopeMatch(double maxSlope, double minSlope)
        {
            if (maxSlope < minSlope || minSlope < 0.0 || maxSlope > 90.0) throw new ArgumentException("MapScalarクラスのGetSlopeMatch()にてエラーがスローされました。引数が不正です。");
            float[,] newMap = new float[this.d_map.GetLength(0), this.d_map.GetLength(1)];
            int x_limit = this.Size.x;
            int y_limit = this.Size.y;
            var feature = new MapLocalFeature<T>(this);                 // 特徴を計算してくれるクラスを宣言

            Parallel.For(0, x_limit, x =>                               // 配列に格納
            {
                Parallel.For(0, y_limit, y =>
                {
                    var slopeTuple = feature.GetSlope(x, y);
                    if (slopeTuple != null)
                    {
                        if (slopeTuple.Item1 >= minSlope && slopeTuple.Item1 <= maxSlope)
                            newMap[x, y] = (float)1.0;
                        else
                            newMap[x, y] = (float)0.0;
                    }
                });
            });

            MapScalar<float> map = new MapScalar<float>(this.Field);    // マップの座標をセット
            map.SetMapData(newMap);                                     // マップの中身をセット
            return map;
        }
        /// <summary>
        /// 指定傾斜角範囲にある地点の座標を配列で返す
        /// </summary>
        /// <param name="maxSlope">最大傾斜角[deg]</param>
        /// <param name="minSlope">最小傾斜角[deg]</param>
        /// <returns>該当した座標の配列</returns>
        public Pos[] GetSlopeMatchArray(double maxSlope, double minSlope)
        {
            List<Pos> ans = new List<Pos>(0);
            int x_limit = this.Size.x;
            int y_limit = this.Size.y;
            var matchMap = this.GetMapSlopeMatch(maxSlope, minSlope);

            Parallel.For(0, x_limit, x =>                               // Listに格納
            {
                Parallel.For(0, y_limit, y =>
                {
                    if (matchMap[x, y] != 0.0)
                    {
                        Monitor.Enter(ans);                             // 通常はフラグの立っている件数の方が圧倒的に少ないはずなので、普通のfor文よりもこの方が速いはず。
                        ans.Add(new Pos(x, y));
                        Monitor.Exit(ans);
                    }
                });
            });
            return ans.ToArray();
        }
        /// <summary>
        /// 指定値にマッチする地点の座標（マップのローカル座標）を配列で返します。
        /// <para>GetMapMatchを使用して、フラグの立った地点を配列として返します。</para>
        /// <para>走査範囲については、fieldで指定して下さい。</para>
        /// </summary>
        /// <param name="value">閾値</param>
        /// <param name="field">マップ中の走査範囲領域</param>
        /// <returns>マッチした地点の座標を配列にしたもの</returns>
        public Pos[] GetMatchPosArray(T value, MapField field)
        {
            field = field.And(new MapField(this.Size));                                         // 領域を本インスタンスに合わせて調整（領域が縮小することも有り得る）
            List<Pos> ans = new List<Pos>(0);
            int x_limit = field.LowerRight.x + 1;
            int y_limit = field.LowerRight.y + 1;
            var matchMap = this.GetMapMatch(value, field);

            Parallel.For(field.UpperLeft.x, x_limit, x =>                                       // Listに格納
            {
                Parallel.For(field.UpperLeft.y, y_limit, y =>
                {
                    if (matchMap[x, y] != 0.0)
                    {
                        Monitor.Enter(ans);                                                     // 通常はフラグの立っている件数の方が圧倒的に少ないはずなので、普通のfor文よりもこの方が速いはず。
                        ans.Add(new Pos(x, y));
                        Monitor.Exit(ans);
                    }
                });
            });
            return ans.ToArray();
        }
        /// <summary>
        /// 指定値にマッチする地点の座標（マップのローカル座標）を配列で返します。
        /// <para>GetMapMatchを使用して、フラグの立った地点を配列として返します。</para>
        /// <para>走査範囲はマップ全体となります。</para>
        /// </summary>
        /// <param name="value">閾値</param>
        /// <returns>マッチした地点の座標を配列にしたもの</returns>
        public Pos[] GetMatchPosArray(T value)
        {
            return this.GetMatchPosArray(value, new MapField(new Pos(), this.Size));
        }
        /// <summary>
        /// マップを画像として保存する
        /// <para>保存形式はImageFormatプロパティを通して設定可能です。</para>
        /// <para>
        /// 個人的都合により、値が非値もしくは0.0以下の箇所では背景として黒色を採用しています。
        /// これは使いにくする仕様になっていますので、その内カラ―スケールを導入するかもしれません。
        /// </para>
        /// <para>
        /// ファイルの出力には実時間で通常2秒以上かかりますのでTaskを利用して処理がすぐ戻るようにしています。
        /// 処理が戻った時点ではファイルができていないことにご注意ください。
        /// </para>
        /// </summary>
        /// <param name="fname">ファイル名</param>
        /// <param name="thresholdOfBack">背景色とする閾値</param>
        /// <returns>保存したファイルのフルパス</returns>
        public string SaveAsImage(string fname, T thresholdOfBack)
        {
            string cd = Directory.GetCurrentDirectory();                        // カレントディレクトリ（作業フォルダ）を取得
            string fileName = cd + @"\" + fname;
            Colormap myColorMap = new Colormap();
            int x_width = this.Size.x;
            int y_height = this.Size.y;
            T max = this.Max;
            T min = this.Min;
            double scaleSize = Convert.ToDouble(Map<T>.Sub(max, min));
            int resized_y = (int)(this.MeshAspectRatio * (double)y_height);     // アスペクト比を考慮して、高さを調整する

            var tsk = Task.Factory.StartNew(() =>
            {
                Color[,] img = new Color[x_width, resized_y];
                Parallel.For(0, x_width, x =>                                   // 配列に色を代入
                {
                    Parallel.For(0, resized_y, y =>
                    {
                        int origin_y = y_height * y / resized_y;
                        double diff = Convert.ToDouble(Map<T>.Sub(this.d_map[x, origin_y], min));
                        double scale = diff / scaleSize;
                        if (scale < 0.0 || double.IsNaN(scale))
                            scale = 0.0;
                        else if (scale > 1.0)
                            scale = 1.0;
                        Color color;
                        if (scale == 0.0 || this.d_map[x, origin_y].CompareTo(thresholdOfBack) <= 0.0)
                            color = Color.Black;                                // 背景は黒
                        else
                            color = myColorMap.GetColor(scale);
                        img[x, y] = color;
                    });
                });

                BmpGenerator ge = new BmpGenerator();                           // 作った配列情報を用いて画像を作成する
                ge.MakeBmp(img);
                Bitmap bitmap = ge.Bitmap;
                bitmap.Save(fileName, this.ImageFormat);                        // bitmapオブジェクトのデータをbmpファイルに保存
            });
            return fileName;
        }
        #endregion
        #region "コンストラクタ関係"
        /// <summary>
        /// 共通した初期化項目
        /// <para>出力画像のフォーアットをビットマップに指定します。</para>
        /// </summary>
        protected void CommonInit()
        {
            this.ImageFormat = System.Drawing.Imaging.ImageFormat.Bmp;
            return;
        }
        /// <summary>
        /// マップの領域を指定してインスタンスを生成するコンストラクタ
        /// <para>
        /// 座標計算を特に使用しない場合を想定しています。
        /// マップのサイズはマップデータの代入時に決定されます。
        /// また、この時にマップデータのインスタンスが確保されます。
        /// </para>
        /// </summary>
        /// <param name="field">マップの領域情報</param>
        public MapScalar(RectangleField field)
            : base(field)
        {
            this.CommonInit();
            return;
        }
        /// <summary>
        /// 領域の角の座標とマップのサイズ（ピクセル数）を指定することでマップを生成するコンストラクタ
        /// </summary>
        /// <param name="field">マップの領域情報</param>
        /// <param name="_size">縦横のマップサイズ</param>
        public MapScalar(RectangleField field, Pos _size)
            : base(field, _size)
        {
            this.CommonInit();
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
        public MapScalar(RectangleField field, Length _meshSize)
            : base(field, _meshSize)
        {
            this.CommonInit();
            return;
        }
        /// <summary>
        /// コピーコンストラクタ
        /// <para>パレメータを始め全てのパラメータをコピーしたインスタンスを生成します。</para>
        /// </summary>
        /// <param name="map">コピー元のマップ</param>
        public MapScalar(MapScalar<T> map)
            : base(map)
        {
            this.CommonInit();
            this.imageFormat = map.imageFormat;
            return;
        }
        /// <summary>
        /// デスコンストラクタ
        /// </summary>
        ~MapScalar()
        {
            this.d_map = null;
        }
        #endregion
    }
}
