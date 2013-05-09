/*****************************************************
 * MapGeneric.cs
 * ラスタ形式の地図情報を扱うクラスです。
 * ジェネリックを利用していますので、任意の型の値を格納できます。
 * 比較系のメソッドは実装していません。一致判定等を利用したい場合はMapGenericCmpクラスをご利用ください。
 * 加算に関するメソッドを実装していますが、利用している型が加算（+演算子）をサポートしていない場合はエラーとなります。
 * 
 * マップの取り扱い注意
 * 1)本クラスのコンストラクタ生成時には必ずマップの右上と左下の座標（緯度・経度）をセットして下さい。
 * 
 * 　開発者：K.Morishita (Kumamoto-University Japan @2012)
 * 　動作環境：.NET Framework 4
 * 　開発履歴：
 * 　        2012/9/22 　(US time) 旧Mapクラスを基に、新規作成
 * 　        2012/9/29   (JST) 整備が一応完了
 * 　                    旧Mapクラスとその開発経緯に関しては、DigitalElevationModel名前空間にあるMapDem.csをご覧ください。
 * 　        2012/10/3   コメントを訂正
 * 　        2012/10/4   コメントを訂正
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
using GenericOperator;          // 演算子のために必要
 
namespace GIS.MapBase
{
    /// <summary>
    /// ラスタ形式の地図情報を扱うクラス
    /// <para>ジェネリックを利用していますので、任意の型の値を格納できます。</para>
    /// <para>比較系のメソッドは実装していません。等値・大小判定等を利用したい場合はMapGenericCmpクラスをご利用ください。</para>
    /// <para>加算に関するメソッドを実装していますが、利用している型が加算（+演算子）をサポートしていない場合はエラーとなります。</para>
    /// <para>
    /// 要素を2次元配列として表現しています。
    /// 配列は[x,y]で宣言しており、画像でたとえると[0,0]が画面の左上、[x_max, y_max]が画面の右下としています。
    /// 画面の上が北で、右が東です。
    /// 東西方向をx、南北方向をyで表現しています。
    /// xは増えるほどより東に位置することを表し、yは増えるほどより南に位置することを表します。
    /// </para>
    /// </summary>
    public class Map<T> : IAddressChanger
    {
        #region /*　メンバ変数　***************************************************************/
        /// <summary>
        /// マップの領域
        /// </summary>
        private RectangleField field;
        /// <summary>
        /// マップのサイズ
        /// </summary>
        protected Pos size;
        /// <summary>
        /// マップサイズに、newとSetMap間で不一致があるとtrue
        /// </summary>
        private Boolean errorSize = false;
        /// <summary>
        /// メッシュの平均的サイズ
        /// <para>緯度方向・経度方向のメッシュサイズとする。単位は[m]。</para>
        /// </summary>
        private Length meshSise = new Length();
        /// <summary>
        /// ラスタのマップデータ
        /// </summary>
        protected T[,] d_map;
        #endregion
        #region /*　プロパティ　***************************************************************/
        /// <summary>
        /// マップのサイズ
        /// <para>メッシュの数でカウントしたマップのサイズを返します。</para>
        /// </summary>
        public Pos Size 
        { 
            get {
                return this.size; 
            }
            protected set {
                this.size = value;
            }
        }
        /// <summary>
        /// マップがセット済みかどうかをtrue or falseで返す。
        /// <para>セットされていればtrue。</para>
        /// </summary>
        public Boolean IsMapSet
        {
            get { 
                if(this.d_map != null)
                    return true;
                else
                    return false;
            }
        }
        /// <summary>
        /// マップのサイズがセットされているかを確認する。
        /// セットされていればtrue。
        /// </summary>
        public Boolean IsSizeSet
        {
            get
            {
                Boolean ans = false;

                if (!(this.Size.x == 0 || this.Size.y == 0)) ans = true;    // マップが2次元として大きさを持っていればtrue
                return ans;
            }
        }
        /// <summary>
        /// メッシュサイズ
        /// <para>南北方向・東西方向のメッシュの平均的サイズ[m]です。</para>
        /// </summary>
        public Length MeshSize 
        { 
            get { return this.meshSise; }
            protected set 
            {
                this.meshSise = value;
            }
        }
        /// <summary>
        /// newでセットしたマップサイズとSetMapでセットされたサイズ間に差があるとtrueとなる。
        /// </summary>
        public Boolean IsErrorSize 
        {
            get { 
                return this.errorSize; 
            }
        }
        /// <summary>
        /// マップの領域（緯度・経度）
        /// </summary>
        public RectangleField Field 
        { 
            set {
                if (this.IsSizeSet)                                         // 可能ならメッシュサイズを計算する
                {
                    this.MeshSize = this.GetMeshSize(value, this.Size);
                }
                this.field = value;
            }
            get { return this.field; }
        }
        /// <summary>
        /// マップのメッシュ数
        /// </summary>
        public int Length 
        { 
            get { return this.d_map.GetLength(0) * this.d_map.GetLength(1); }
        }
        /// <summary>
        /// メッシュのアスペクト比
        /// <para>N / E で定義している。つまり、1以上なら南北方向に長いメッシュである。</para>
        /// </summary>
        public double MeshAspectRatio 
        { 
            get 
            { 
                return this.MeshSize.N / this.MeshSize.E; 
            }
        }
        /// <summary>
        /// 本マップの中央の座標
        /// </summary>
        public GNSS.Blh Center
        {
            get { return this.Field.UpperRight.GetMedian(this.Field.LowerLeft); }
        }
        #endregion
        /*　インデクサ　*********************************************************************/
        /// <summary>
        /// 指定座標の値（例えば高度）を取得orセット
        /// <para>座標はマップのローカル座標（0～this.Size.x or y - 1）</para>
        /// <para>[0 ,0]がマップの左上を指し、xは右向きに正，yは下向きに正となります。</para>
        /// </summary>
        /// <param name="x">x座標</param>
        /// <param name="y">y座標</param>
        /// <returns>マップの値</returns>
        public T this[int x, int y]
        {
            get {
                if (x >= 0 && y >= 0 && x < this.Size.x && y < this.Size.y)
                    return this.d_map[x, y];
                else
                    throw new SystemException("引数が範囲内にありません。");
            }
            set { 
                if (x >= 0 && y >= 0 && x < this.Size.x && y < this.Size.y)
                    this.d_map[x, y] = value;
                else
                    throw new SystemException("引数が範囲内にありません。");
            }
        }
        #region /* ジェネリックのための演算子 ***********************************************/
        /// <summary>
        /// 足し算
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static T Add(T x, T y) { return Operator<T>.Add(x, y); }
        /// <summary>
        /// 引き算
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected static T Sub(T x, T y) { return Operator<T>.Subtract(x, y); }
        /// <summary>
        /// 掛け算
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected static T Mul(T x, T y) { return Operator<T>.Multiply(x, y); }
        /// <summary>
        /// 割り算
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected static T Div(T x, T y) { return Operator<T>.Divide(x, y); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        protected static T Neg(T x) { return Operator<T>.Negate(x); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        protected static T Acc(T x, T y, T z, T w) { return Operator<T>.ProductSum(x, y, z, w); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        protected static T Det(T x, T y, T z, T w) { return Operator<T>.ProductDifference(x, y, z, w); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected static T Norm(T x, T y) { return Operator<T>.ProductSum(x, y, x, y); }
        #endregion
        #region /*　メンバ関数（メソッド）***************************************************/
        /// <summary>
        /// 座標（緯度・経度）に指定された値を加算する
        /// </summary>
        /// <param name="_position">指定座標（緯度・経度）</param>
        /// <param name="value">加算値</param>
        public void AddValue(Blh _position, T value)
        {
            Pos address = new Pos();

            if (this.IsMapSet && this.Useful(_position))            // マップがセットしてあり、且つ指定座標がマップの範囲内であればOK 
            {
                address = this.Position2XYaddress(_position);       // アドレスを計算
                if (address.x >= 0 && address.x < this.Size.x && address.y >= 0 && address.y < this.Size.y)
                    this.d_map[address.x, address.y] = Map<T>.Add(this.d_map[address.x, address.y], value);      // アドレスが有効範囲であれば、値を加算する
            }
            return;
        }
        /// <summary>
        /// 座標（マップ内のローカル座標）に指定された値を加算する
        /// <para>利用している型が加算をサポートしていない場合はエラーとなります。</para>
        /// </summary>
        /// <param name="_position">指定座標（マップ内のローカル座標）</param>
        /// <param name="value">加算値</param>
        public void AddValue(Pos _position, T value)
        {
            if (this.IsMapSet && this.Useful(_position))            // マップがセットしてあり、且つ指定座標がマップの範囲内であればOK 
            {
                this.d_map[_position.x, _position.y] = Map<T>.Add(this.d_map[_position.x, _position.y], value);      // アドレスが有効範囲であれば、値を加算する
            }
            else
            {
                throw new SystemException("マップが初期化されておりません。");
            }
            return;
        }
        /// <summary>
        /// 座標（緯度・経度の配列）に指定された値（配列）を加算する
        /// <para>利用している型が加算をサポートしていない場合はエラーとなります。</para>
        /// </summary>
        /// <param name="_position">指定座標配列（緯度・経度）</param>
        /// <param name="values">加算値の配列（座標毎に個別に指定可能です）</param>
        public void AddValues(Blh[] _position, T[] values)
        {
            Parallel.For(0, _position.Length, i =>
            {
                this.AddValue(_position[i], values[i]);
            });
            return;
        }
        /// <summary>
        /// 座標（マップ内のローカル座標）に指定された値（配列）を加算する
        /// <para>利用している型が加算をサポートしていない場合はエラーとなります。</para>
        /// </summary>
        /// <param name="_position">指定座標配列</param>
        /// <param name="values">加算値の配列（座標毎に個別に指定可能です）</param>
        public void AddValues(Pos[] _position, T[] values)
        {
            Parallel.For(0, _position.Length, i =>
            {
                this.AddValue(_position[i], values[i]);
            });
            return;
        }
        /// <summary>
        /// 指定された座標を中心とした、指定された半径 mを収めることができる領域を返す
        /// <para>返す領域はマップの領域から外に出ることも有り得ます。</para>
        /// </summary>
        /// <param name="center">中心座標</param>
        /// <param name="radius">半径[m]</param>
        /// <returns>指定された範囲が収まるMapField</returns>
        public MapField GetField(Pos center, double radius)
        {
            int delta_x = (int)(radius / this.MeshSize.E);
            int delta_y = (int)(radius / this.MeshSize.N);
            Pos delta = new Pos(delta_x, delta_y);
            return new MapField(center + delta, center - delta);
        }
        /// <summary>
        /// マップを2次元配列で返す
        /// <para>
        /// インスタンスは新たに生成しないので、高速アクセス・操作が可能です。
        /// ただし、値をいじるとマップも変わるので注意して下さい。</para>
        /// </summary>
        public T[,] GetMap()
        {
            return this.d_map;
        }
        /// <summary>
        /// マップを指定した領域に合わせて切り出します
        /// </summary>
        /// <param name="field">領域</param>
        /// <returns>Mapオブジェクト<para>生成に失敗すると、nullを返します。</para></returns>
        public Map<T> CropMap(RectangleField field)
        {
            if (field.AreaIsZero == false)
            {
                Pos pul = new Pos();
                Pos plr = new Pos();

                plr = this.Position2XYaddress(field.LowerRight);
                pul = this.Position2XYaddress(field.UpperLeft);
                MapField mf = new MapField(plr, pul);

                var _field = mf.And(new MapField(this.Size));                       // 領域を本インスタンスに合わせて調整
                if (_field.AreaIsZero == false)
                {
                    T[,] newMap = new T[_field.Width, _field.Height];
                    int x_limit = _field.Width;
                    int y_limit = _field.Height;

                    Parallel.For(0, x_limit, xl =>                                      // コピー
                    {
                        Parallel.For(0, y_limit, yl =>
                        {
                            int x = xl + pul.x;
                            int y = yl + pul.y;
                            newMap[xl, yl] = this.d_map[x, y];
                        });
                    });
                    var pulDash = pul - new Pos(1, 1);                                  // 一つ外側の座標を求める
                    var plrDash = plr + new Pos(1, 1);
                    var blhPulCorner = this.XYaddress2Position(pul).GetMedian(this.XYaddress2Position(pulDash));    // コーナーの座標を計算する
                    var blhPlrCorner = this.XYaddress2Position(plr).GetMedian(this.XYaddress2Position(plrDash));

                    Map<T> map = new Map<T>(new RectangleField(blhPulCorner, blhPlrCorner));  // マップの座標をセット
                    map.SetMapData(newMap);                                             // マップの中身をセット
                    return map;
                }
                else
                    return null;
            }
            else
                return null;
        }
        /// <summary>
        /// 本マップの情報を文字列にして返す
        /// <para>
        /// 確認用です。
        /// 1要素毎に改行しています。
        /// 配列への格納順はマップの左上から右の方へ進めて右端で左端に返す様にしています。
        /// </para>
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(13375000);                      // 予め大きなメモリ容量を確保しておく

            if (this.IsMapSet)
            {
                for (int y = 0; y < this.Size.y; y++)
                {
                    for (int x = 0; x < this.Size.x; x++)
                    {
                        sb.Append(this.d_map[x, y].ToString()).Append("\n");    // stringを使うより、断然高速
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 本マップの情報を、XMLフォーマットの文字列にして返す。
        /// <para>
        /// マップとしてはデフォルト形式です。
        /// 確認用です。
        /// </para>
        /// </summary>
        public string ToStringAsXml()
        {
            StringBuilder sb = new StringBuilder(3375000);                      // 予め大きなメモリ容量を確保しておく

            if (this.IsMapSet)
            {
                sb.Append("<gml:high>").Append((this.Size.x - 1).ToString()).Append(" ").Append((this.Size.y - 1).ToString()).Append("</gml:high>\n<gml:tupleList>\n");
                for (int y = 0; y < this.Size.y; y++)
                {
                    for (int x = 0; x < this.Size.x; x++)
                    {
                        sb.Append("other,").Append(this.d_map[x, y].ToString()).Append("\n");    // stringを使うより、断然高速
                    }
                }
                sb.Append("</gml:tupleList>\n");
            }
            return sb.ToString();
        }
        /// <summary>
        /// 本マップの情報を、Rコマンダー用フォーマットの文字列にして返す
        /// <para>
        /// 緯度・経度とマップ情報をカンマ区切りで出力します。
        /// 確認用です。
        /// </para>
        /// </summary>
        public string ToStringForRcmdr()
        {
            StringBuilder sb = new StringBuilder(3375000);                      // 予め大きなメモリ容量を確保しておく

            if (this.IsMapSet)
            {
                sb.Append("Longitude,Latitude,Value\n");
                for (int y = 0; y < this.Size.y; y++)
                {
                    for (int x = 0; x < this.Size.x; x++)
                    {
                        Pos add = new Pos(x, y);
                        Blh pos = this.XYaddress2Position(add);
                        sb.Append(pos.L.ToString()).Append(",").Append(pos.B.ToString()).Append(",").Append(this.d_map[x, y].ToString()).Append("\n");    // stringを使うより、断然高速
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 指定ファイル名でデータを保存する
        /// <para>KMLに加工するメソッドもその内作りたいなぁ。</para>
        /// </summary>
        /// <param name="fname">ファイル名</param>
        public void SaveFileAsXml(string fname)
        {
            using (System.IO.StreamWriter fwriter = new System.IO.StreamWriter(fname, false))
            {
                try
                {
                    fwriter.Write(this.ToStringAsXml());                              // 文字列化したデータを保存する。
                }
                catch 
                {
                    // エラー処理があれば記述
                }
            }
            return;
        }
        /// <summary>
        /// 指定ファイル名でデータを保存する
        /// <para>KMLに加工するメソッドもその内作りたいなぁ。</para>
        /// </summary>
        /// <param name="fname">ファイル名</param>
        public void SaveFileForRcmdr(string fname)
        {
            using (System.IO.StreamWriter fwriter = new System.IO.StreamWriter(fname, false))
            {
                try
                {
                    fwriter.Write(this.ToStringForRcmdr());                              // 文字列化したデータを保存する。
                }
                catch
                {
                    // エラー処理があれば記述
                }
            }
            return;
        }
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// 渡された緯度経度がこのマップの範囲内にあるかどうかをBoolで返す。
        /// </summary>
        /// <param name="_position">検査座標</param>
        /// <returns>trueなら使用可能</returns>
        public Boolean Useful(Blh _position)
        {
            if (_position.B <= this.Field.UpperRight.B &&
                _position.B >= this.Field.LowerLeft.B &&
                _position.L <= this.Field.UpperRight.L &&
                _position.L >= this.Field.LowerLeft.L)
            {
                return true;
            }else{
                return false;
            }
        }
        /// <summary>
        /// 渡されたローカル座標がこのマップの範囲内にあるかどうかをBoolで返す。
        /// </summary>
        /// <param name="_position">検査座標</param>
        /// <returns>trueなら使用可能</returns>
        /// <exception cref="SystemException">マップサイズがセットされていなければスロー</exception>
        public Boolean Useful(Pos _position)
        {
            if (this.IsSizeSet)
            {
                if (_position.x >= 0 && _position.x < this.Size.x && _position.y >= 0 && _position.y < this.Size.y)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                throw new SystemException("マップサイズがセットされていません。初期化が必要です。");
            }
        }
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// 指定座標のx,yアドレスを返す
        /// <para>
        /// マップの要素番号は0～size-1であることにご注意下さい。
        /// 本メソッドはマップ領域外の座標を指定された場合でも本マップ内の相対アドレスを返します。
        /// </para>
        /// </summary>
        /// <param name="position">指定座標（緯度・経度）</param>
        /// <returns>x,yアドレス</returns>
        public Pos Position2XYaddress(Blh position)
        {
            Pos ans = new Pos();

            if (this.IsSizeSet)                             // マップサイズがセットしてあることを確認（マップサイズが不明だと内部アドレスを計算できない）
            {
                ans.y = (int)((double)this.Size.y / (this.Field.LowerLeft.B - this.Field.UpperRight.B) * (position.B - this.Field.UpperRight.B));       // 将来、C#のキャストの仕様が変更になったらバグとなるので注意(2011/7)
                ans.x = (int)((double)this.Size.x / (this.Field.UpperRight.L - this.Field.LowerLeft.L) * (position.L - this.Field.LowerLeft.L));
            }
            else 
            {
                throw new SystemException("マップにサイズが定義されていません。初期化が必要です。");
            }
            return ans;
        }
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// 指定アドレスにおけるメッシュ中央の緯度・経度を返す
        /// <para>指定アドレスは、マップの範囲を超えていてもOKです。</para>
        /// </summary>
        /// <param name="address">x,yで指定される座標</param>
        /// <returns>座標（緯度・経度）<para>マップ領域及びメッシュサイズが未定義であって、座標を定義不可能な場合はnew Blh()を返します。</para></returns>
        public Blh XYaddress2Position(Pos address)
        {
            Blh ans = new Blh();

            if (this.IsSizeSet)                             // マップサイズがセットしてあることを確認（マップサイズが不明だと座標を計算できない）
            {
                ans.B = (this.Field.LowerLeft.B - this.Field.UpperRight.B) / (double)this.Size.y * ((double)address.y + 0.5d) + this.Field.UpperRight.B;
                ans.L = (this.Field.UpperRight.L - this.Field.LowerLeft.L) / (double)this.Size.x * ((double)address.x + 0.5d) + this.Field.LowerLeft.L;
            }
            return ans;
        }
        /// <summary>
        /// ローカルアドレスの配列を緯度・経度の配列へ変換して返す
        /// </summary>
        /// <param name="posArray">ローカルアドレスの配列</param>
        /// <returns>緯度・経度に変換した配列</returns>
        public Blh[] ToBLHarray(Pos[] posArray)
        {
            GNSS.Blh[] blhArray = new Blh[posArray.Length];
            for (int i = 0; i < blhArray.Length; i++) blhArray[i] = this.XYaddress2Position(posArray[i]);
            return blhArray;
        }
        
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// 指定座標の属するメッシュの右上・左上の座標を返す
        /// <para>任意に作出されたマップの位置を本クラスのメッシュ位置と一致させる用途での仕様を想定しています。</para>
        /// </summary>
        /// <param name="_position">指定座標（緯度・経度）</param>
        /// <returns>メッシュの右上・左上の座標（緯度・経度）</returns>
        public RectangleField GetMeshCorner(Blh _position)
        {
            RectangleField ans = new RectangleField();
            if (this.IsSizeSet)                                 // マップサイズがセットしてあることを確認（マップサイズが不明だと内部アドレスを計算できない）
            {
                Pos add = this.Position2XYaddress(_position);
                Blh centerPos = this.XYaddress2Position(add);     // 指定メッシュの中央位置における緯度・経度を取得
                Blh deltaPos = new Blh();
                deltaPos.B = (this.Field.UpperRight.B - this.Field.LowerLeft.B) / ((double)this.Size.y * 2.0);   // メッシュ中央から、メッシュ境界までの距離を緯度・経度で計算
                deltaPos.L = (this.Field.UpperRight.L - this.Field.LowerLeft.L) / ((double)this.Size.x * 2.0);
                Blh upperR = centerPos + deltaPos;              // 右上の座標を計算
                Blh underL = centerPos - deltaPos;              // 左下の座標を計算
                ans = new RectangleField(upperR, underL);
            }
            return ans;
        }
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// 指定された座標を中心として、指定された相対アドレスの緯度と経度を返します。
        /// </summary>
        /// <param name="_center_position">指定座標</param>
        /// <param name="_delta_address">相対座標（メッシュ数でカウントする）</param>
        /// <returns>指定された座標のマップ情報を返す</returns>
        public Blh GetDifferentialPosition(Blh _center_position, Pos _delta_address)
        {
            Blh ans = new Blh();

            if (this.IsSizeSet)                             // マップサイズがセットしてあることを確認（マップサイズが不明だと内部アドレスを計算できない）
            {
                Pos current_pos = this.Position2XYaddress(_center_position);
                current_pos += _delta_address;              // 指定されている相対座標を加算する
                ans = this.XYaddress2Position(current_pos);   // 緯度・経度を求める
            }
            return ans;
        }
        /// <summary>
        /// 2地点間の距離[m]を返す
        /// </summary>
        /// <param name="p1">地点1</param>
        /// <param name="p2">地点2</param>
        /// <returns>2地点間の距離[m]</returns>
        public double GetDistance(Pos p1, Pos p2)
        {
            Pos diff = p1 - p2;
            return Math.Sqrt(Math.Pow(diff.x * this.MeshSize.E, 2.0) + Math.Pow(diff.y * this.MeshSize.N, 2.0));
        }
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// 指定座標のマップデータを返す
        /// <para>指定座標に該当データが存在しない場合はfloat.NaNを返します。</para>
        /// <para>
        /// 計算式の関係上、東経を基準にしているので西経のデータを扱う場合は東経に変換してから使用する事。
        /// 同様に、南緯は北緯に変換して使用して下さい。
        /// 例：西経15度=>東経-15度
        /// </para>
        /// </summary>
        /// <param name="_position">座標（lat,lon）</param>
        /// <returns>指定座標のマップデータ<para>float.NaN：該当なし</para></returns>
        public T GetValue(Blh _position)
        {
            T ans = default(T);

            if (this.IsMapSet)                                                      // マップがセットされていなければ、渡すデータが無い（それに、エラーが出る）
            {
                if (this.Useful(_position))                                         // 指定座標が本マップに適用可能なら、値を抽出する
                {
                    Pos address = this.Position2XYaddress(_position);               // 指定座標からマップ内アドレスを計算
                    ans = this.GetValue(address);
                }
            }
            else
            {
                throw new SystemException("マップがセットされていません。初期化が必要です。");
            }
            return ans;
        }
        /// <summary>
        /// 指定座標のマップデータを返す
        /// <para>
        /// マップ内のローカル座標を指定して下さい。
        /// </para>
        /// </summary>
        /// <param name="_pos">マップ内のローカル座標</param>
        /// <returns>指定座標があれば指定されたマップ内の値を返す。無ければ0もしくはnullを返す。</returns>
        public T GetValue(Pos _pos)
        {
            T ans = default(T);

            if (this.IsMapSet)                                              // マップがセットされていなければ、渡すデータが無い（それに、エラーが出る）
            {
                if (this.Useful(_pos))                                      // 指定座標が本マップに適用可能なら、値を抽出する
                {
                    ans = this.d_map[_pos.x, _pos.y];
                }
                else
                {
                    throw new SystemException("範囲外の座標が指定されました。");
                }
            }
            else
            {
                throw new SystemException("マップがセットされていません。初期化が必要です。");
            }
            return ans;
        }
        /// <summary>
        /// マップのサイズと角の座標からメッシュのサイズを計算して返す
        /// </summary>
        /// <param name="corner">角の座標値</param>
        /// <param name="mapSize">マップのサイズ</param>
        /// <returns>メッシュサイズ</returns>
        private Length GetMeshSize(RectangleField corner, Pos mapSize)
        {
            Blh _lower = corner.LowerLeft;
            Blh _upper = corner.UpperRight;
            Length _distance = _lower.GetDistance(_upper);                  // 2地点間の距離を求める。 10cm程度の誤差は無視しているので注意。
            return new Length(_distance.E / (double)mapSize.x, _distance.N / (double)mapSize.y); // メッシュのサイズを計算
        }
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// マップデータをセットする
        /// <para>データはディープコピーされます。</para>
        /// </summary>
        /// <param name="_map">マップのラスタデータ</param>
        public void SetMapData(T[,] _map)
        {
            Pos _size = new Pos(_map.GetLength(0), _map.GetLength(1));
            if (_size == new Pos()) throw new SystemException("マップのサイズが0です。");
            if (this.Size != _size)
            {
                //Console.WriteLine("MapBase.SetMapData(): マップを内部データに取り込む際に、セットマップサイズが異なるorセットされていなかったことを検出しました。引数の大きさに合わせてサイズを調整します。");
                this.Size = _size;                                          // サイズを代入
                this.MeshSize = this.GetMeshSize(this.Field, _size);        // メッシュサイズを計算して代入する
            }

            //this.d_map = _map;                                            // 実はシャローコピーでも処理時間はさほど変わらない。

            if (this.IsMapSet == false) this.d_map = new T[this.Size.x, this.Size.y]; // メモリ領域を確保
            // マップをセット           
            Parallel.For(0, this.Size.x, x =>                               // データをコピー
            {
                Parallel.For(0, this.Size.y, y=>
                {
                    this.d_map[x, y] = _map[x, y];
                });
            });
            return;
        }
        /// <summary>
        /// マップの初期化メソッド
        /// <para>
        /// マップサイズを定義済みであれば、マップの初期化を行います。
        /// マップサイズが定義済みかどうかはIsSizeSetメソッドを参照してご確認ください。
        /// なお、マップデータはデフォルトでは0.0fで上書きされます。
        /// </para>
        /// </summary>
        /// <param name="_value">初期化で埋めたい数値</param>
        public void Initialize(T _value)
        {
            if (this.IsSizeSet)
            {
                if (this.IsMapSet == false) this.d_map = new T[this.Size.x, this.Size.y];   // メモリ領域を確保
                Parallel.For(0, this.Size.x, x =>
                {
                    for (int y = 0; y < this.Size.y; y++)
                    {
                        this.d_map[x, y] = _value;
                    }
                });
            }
            return;
        }
        #endregion
        #region "コンストラクタ関係"
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// マップの領域を指定してインスタンスを生成するコンストラクタ
        /// <para>
        /// 座標計算を特に使用しない場合を想定しています。
        /// マップのサイズはマップデータの代入時に決定されます。
        /// また、この時にマップデータのインスタンスが確保されます。
        /// </para>
        /// </summary>
        /// <param name="field">マップの領域情報</param>
        public Map(RectangleField field)
        {
            this.Size = new Pos();                                  // サイズの初期化だけはしておく
            this.Field = field;
            return;
        }
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// 領域の角の座標とマップのサイズ（ピクセル数）を指定することでマップを生成するコンストラクタ
        /// </summary>
        /// <param name="field">マップの領域情報</param>
        /// <param name="_size">縦横のマップサイズ</param>
        public Map(RectangleField field, Pos _size)
        {
            this.Size = _size;
            this.MeshSize = this.GetMeshSize(field, this.Size);
            this.Field = field;                                     // 各種データを格納
            this.d_map = new T[this.Size.x, this.Size.y];           // マップの実態を確保
            return;
        }
        /*2011/7/6 動作確認済み*/
        /// <summary>
        /// 領域の角の座標とメッシュサイズを指定することでマップを生成するコンストラクタ
        /// <para>
        /// マップの境界値が曖昧だけども任意のマップを生成したいという場合を想定しています。
        /// なお、角の座標値はメッシュサイズに合わせて微調整されます。
        /// </para>
        /// </summary>
        /// <param name="field">作りたいマップの領域情報（緯度・経度）</param>
        /// <param name="_meshSize">メッシュサイズ[m]</param>
        public Map(RectangleField field, Length _meshSize)
        {
            Blh _lower = field.LowerLeft;
            Blh _upper = field.UpperRight;
            Length _distance = _lower.GetDistance(_upper);                                                      // 2地点間の距離を求める。 高度の効果による10cm程度の誤差は無視しているので注意。
            Pos _size = new Pos((int)((Math.Abs(_distance.E) + 0.001) / _meshSize.E + 1.0d), (int)((Math.Abs(_distance.N) + 0.001) / _meshSize.N));      // マップのサイズを決める

            this.MeshSize = _meshSize;                                                                          // メッシュのサイズ（網目のサイズ==分解能）をコピー
            this.Size = _size;                                                                                  // マップのサイズをセット
            this.d_map = new T[this.Size.x, this.Size.y];
            _upper.L = _meshSize.E * (double)this.Size.x / _lower.GetUnitLengthForEN().E + field.LowerLeft.L;   // マップのサイズから、upperの座標値を修正する
            _upper.B = _meshSize.N * (double)this.Size.y / _lower.GetUnitLengthForEN().N + field.LowerLeft.B;   // 2地点中央の単位長でないのでやっぱり少しずれるけど、まあいいや
            this.Field = new RectangleField(_lower, _upper, AngleUnit.Degree, Datum.WGS84);                     // メッシュ調整のためにupperだけ修正をかけたインスタンスを生成。
            return;
        }
        /// <summary>
        /// コピーコンストラクタ
        /// <para>パレメータを始め全てのパラメータをコピーしたインスタンスを生成します。</para>
        /// </summary>
        /// <param name="map">コピー元のマップ</param>
        public Map(Map<T> map)
        {
            this.field = new RectangleField(map.field);
            this.size = map.size;
            this.errorSize = map.errorSize;
            this.meshSise = map.meshSise;

            if (map.d_map != null)
            {
                T[,] newMap = new T[map.d_map.GetLength(0), map.d_map.GetLength(1)];
                int x_width = map.Size.x;
                int y_height = map.Size.y;

                Parallel.For(0, x_width, x =>
                {
                    Parallel.For(0, y_height, y =>
                    {
                        newMap[x, y] = map.d_map[x, y];                 // 高速性を優先して、インデグサを用いない
                    });
                });
                this.d_map = newMap;
            }
            else
                this.d_map = null;
            return;
        }
        /// <summary>
        /// デスコンストラクタ
        /// </summary>
        ~Map()
        {
            this.d_map = null;
        }
        #endregion
    }
}
