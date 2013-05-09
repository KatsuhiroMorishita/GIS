/*****************************************************
 * Pos.cs
 * 座標を表す構造体です。
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
 * 　                    MapLocalFeatureクラスを本ソースファイルより分離
 * 　        2012/5/4    MapBase.csから移した。
 * ***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.MapBase
{
    /// <summary>
    /// x,yの座標セット構造体
    /// <para>マップのアドレス計算用</para>
    /// <para>相対アドレス計算用に負値にも対応しています。</para>
    /// </summary>
    public struct Pos : IComparable
    {
        /* メンバ変数　************************************/
        /// <summary>アドレスx</summary>
        public int x;
        /// <summary>アドレスy</summary>
        public int y;
        /* 演算子　************************************/
        /// <summary>
        /// 二項+演算子（これで足し算が簡単にできる）
        /// </summary>
        /// <param name="c1">被加算値</param>
        /// <param name="c2">加算値</param>
        /// <returns>2値を加算した結果</returns>
        public static Pos operator +(Pos c1, Pos c2)
        {
            return new Pos(c1.x + c2.x, c1.y + c2.y);
        }
        /// <summary>
        /// 二項-演算子（これで足し算が簡単にできる）
        /// </summary>
        /// <param name="c1">被減算値</param>
        /// <param name="c2">減算値</param>
        /// <returns>2値の引き算の結果</returns>
        public static Pos operator -(Pos c1, Pos c2)
        {
            return new Pos(c1.x - c2.x, c1.y - c2.y);
        }
        //比較演算子の==と!=をオーバーロードする 2011/12/16 ほぼコピペなので少し不安
        /// <summary>
        /// 比較演算子==
        /// </summary>
        /// <param name="c1">比較対象その1</param>
        /// <param name="c2">比較対象その2</param>
        /// <returns>等しければture</returns>
        public static bool operator ==(Pos c1, Pos c2)
        {
            // nullの確認（構造体のようにNULLにならない型では不要）
            if (object.ReferenceEquals(c1, c2))                 // 両方nullか（参照元が同じか）チェックする。(c1 == c2)とすると、無限ループ
            {
                return true;
            }
            if (((object)c1 == null) || ((object)c2 == null))   // どちらかがnullかチェックする。(c1 == null)とすると、無限ループ
            {
                return false;
            }
            return (c1.x == c2.x) && (c1.y == c2.y);
        }
        /// <summary>
        /// 比較演算子!=
        /// </summary>
        /// <param name="c1">比較対象その1</param>
        /// <param name="c2">比較対象その2</param>
        /// <returns>等しくなければture</returns>
        public static bool operator !=(Pos c1, Pos c2)
        {
            return !(c1 == c2);                 // (c1 != c2)とすると、無限ループ
        }
        /// <summary>
        /// ハッシュ値を返す
        /// </summary>
        /// <returns>xとyのXOR結果</returns>
        public override int GetHashCode()
        {
            return this.x ^ this.y;
        }
        /// <summary>
        /// objと自分自身が等価のときはtrueを返す
        /// </summary>
        /// <param name="obj">比較したいオブジェクト</param>
        /// <returns>等価であればtrue</returns>
        public override bool Equals(object obj)
        {
            //objがnullか、型が違うときは、等価でない
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }
            //この型が継承できないクラスや構造体であれば、次のようにできる
            //if (!(obj is TestClass))

            // メンバで比較する
            Pos c = (Pos)obj;
            return (this.x == c.x && this.y == c.y);
        }
        /* プロパティ　************************************/
        /// <summary>
        /// 長さ（原点からの距離）
        /// <para>Math.Sqrt(x^2 + y^2)で定義しました。</para>
        /// </summary>
        /// <returns>長さ</returns>
        public int Length
        {
            get { return (int)Math.Sqrt(this.x * this.x + this.y * this.y); }
        }
        /* メソッド　************************************/
        /// <summary>
        /// 引数で渡した座標との距離を返す
        /// </summary>
        /// <param name="origin">始点</param>
        /// <returns>距離</returns>
        public double GetDistance(Pos origin)
        {
            return Math.Sqrt((this.x - origin.x) * (this.x - origin.x) + (this.y - origin.y) * (this.y - origin.y));
        }
        /// <summary>
        /// 文字列化
        /// <para>デリミタにはカンマを利用します。</para>
        /// </summary>
        /// <returns>メンバ変数x,yを文字列化したしたstring型変数</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(50);                      // 予め大きなメモリ容量を確保しておく

            sb.Append(this.x.ToString()).Append(",").Append(this.y.ToString());
            return sb.ToString();
        }
        /// <summary>
        /// 比較用メソッド
        /// <para>(0,0)からの距離によって大小を判定します。</para>
        /// </summary>
        /// <param name="obj">比較したいPos型変数</param>
        /// <returns>引数の方が大きければ負値を返す</returns>
        public int CompareTo(object obj)
        {
            Pos pos = (Pos)obj;
            return this.Length - pos.Length;
        }
        /// <summary>
        /// 初期値セット機能付きコンストラクタ
        /// </summary>
        /// <param name="x0">xの初期値</param>
        /// <param name="y0">yの初期値</param>
        public Pos(int x0 = 0, int y0 = 0)
        {
            this.x = x0;
            this.y = y0;
        }
    }
}
