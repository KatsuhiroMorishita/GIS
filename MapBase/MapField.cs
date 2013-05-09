/*****************************************************
 * MapField.cs
 * マップ中の領域を表す構造体です。
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
 * 　        2012/5/15   負値、同値へ対応するように変更した。
 * 　                    FixField()を破棄し、代わりに領域の重ね合わせを取るAndを新設した。
 * 　                    And機能のために、上下端や左右端のxとyを公開するプロパティを新設した。
 * 　                    また、思いつきで平行移動させるTranslate()も新設した。
 * 　        2012/5/17   一連の機能の簡単なテストを実施し、バグを修正した。
 * 　                    これまではMapクラス内に宣言されていたが、これをMapBase名前空間へ移した。
 * 　        2012/9/2    WidthとHeightを追加
 * ***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.MapBase
{
    /// <summary>
    /// マップ中の領域を整数値アドレスで表す構造体
    /// <para>Mapクラスのメソッドに対して領域を指定するのに使用することを想定しています。</para>
    /// <para>インスタンス生成後、オブジェクト自身の領域を拡大することはできても縮小はできません。And()をうまく利用してください。</para>
    /// </summary>
    public struct MapField
    {
        /* メンバ変数 ***********************/
        /// <summary>
        /// パラメータ
        /// </summary>
        private FieldParameter param;
        /* プロパティ**********************/
        /// <summary>
        /// 上端のy値
        /// </summary>
        public int UpperY
        {
            get { return this.param.upperY; }
        }
        /// <summary>
        /// 下端のy値
        /// </summary>
        public int LowerY
        {
            get { return this.param.lowerY; }
        }
        /// <summary>
        /// 右端のx値
        /// </summary>
        public int RightX
        {
            get { return this.param.rightX; }
        }
        /// <summary>
        /// 左端のx値
        /// </summary>
        public int LeftX
        {
            get { return this.param.leftX; }
        }
        /// <summary>
        /// 左上の座標
        /// </summary>
        public Pos UpperLeft
        {
            get { return new Pos(this.param.leftX, this.param.upperY); }
        }
        /// <summary>
        /// 右上の座標
        /// </summary>
        public Pos UpperRight
        {
            get { return new Pos(this.param.rightX, this.param.upperY); }
        }
        /// <summary>
        /// 左下の座標
        /// </summary>
        public Pos LowerLeft
        {
            get { return new Pos(this.param.leftX, this.param.lowerY); }
        }
        /// <summary>
        /// 右下の座標
        /// </summary>
        public Pos LowerRight
        {
            get { return new Pos(this.param.rightX, this.param.lowerY); }
        }
        /// <summary>
        /// 面積が存在するかを返す
        /// <para>true: 面積は0。</para>
        /// </summary>
        public Boolean AreaIsZero
        {
            get
            {
                return this.param.AreaIsZero;
            }
        }
        /// <summary>
        /// 領域の幅[point]
        /// </summary>
        public int Width
        {
            get 
            {
                return this.RightX - this.LeftX + 1;
            }
        }
        /// <summary>
        /// 領域の高さ[point]
        /// </summary>
        public int Height
        {
            get 
            {
                return this.LowerY - this.UpperY + 1;
            }
        }
        /* メソッド *************************/
        /// <summary>
        /// 文字列化して返す
        /// <para>フォーマットは、「左上の座標」，「右下の座標」</para>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(50);
            sb.Append(this.UpperLeft.ToString()).Append(",").Append(this.LowerRight.ToString());
            return sb.ToString();
        }
        /// <summary>
        /// 領域を平行移動させる
        /// <para>破壊的メソッド（オブジェクト自身の値を変更します）</para>
        /// </summary>
        /// <param name="shiftAmount">シフト量<para>x成分は正値で右へ，y成分は正値で下方へ領域が平行移動します。</para></param>
        public void Translate(Pos shiftAmount)
        {
            this.param.leftX += shiftAmount.x;
            this.param.lowerY += shiftAmount.y;
            this.param.rightX += shiftAmount.x;
            this.param.upperY += shiftAmount.y;
            return;
        }
        /// <summary>
        /// 任意の地点が領域内にあるか確認する
        /// </summary>
        /// <param name="pos">検査座標</param>
        /// <returns>true: 領域内にある。</returns>
        public Boolean CheckInclusion(Pos pos)
        {
            if (this.UpperY < pos.y && this.LowerY > pos.y && this.LeftX < pos.x && this.RightX > pos.x)
                return true;
            else
                return false;
        }
        /// <summary>
        /// 引数で渡された領域と重ね合わせ、共通の領域を返す
        /// </summary>
        /// <param name="field">ターゲットの領域</param>
        /// <returns>共通領域</returns>
        public MapField And(MapField field)
        {
            List<int> xbuff = new List<int>();
            List<int> ybuff = new List<int>();

            if (this.UpperY <= field.UpperY && this.LowerY >= field.UpperY) ybuff.Add(field.UpperY);    // 2つの領域が重なるかどうかをチェック
            if (this.UpperY <= field.LowerY && this.LowerY >= field.LowerY) ybuff.Add(field.LowerY);
            if (field.UpperY < this.UpperY && field.LowerY > this.UpperY) ybuff.Add(this.UpperY);
            if (field.UpperY < this.LowerY && field.LowerY > this.LowerY) ybuff.Add(this.LowerY);

            if (this.RightX >= field.RightX && this.LeftX <= field.RightX) xbuff.Add(field.RightX);
            if (this.RightX >= field.LeftX && this.LeftX <= field.LeftX) xbuff.Add(field.LeftX);
            if (field.RightX > this.RightX && field.LeftX < this.RightX) xbuff.Add(this.RightX);
            if (field.RightX > this.LeftX && field.LeftX < this.LeftX) xbuff.Add(this.LeftX);

            Pos p1, p2;                                                                                 // 抽出されたz,yの組を2つ作って領域を作る
            if (xbuff.Count >= 2 && ybuff.Count >= 2)
            {
                p1 = new Pos(xbuff[0], ybuff[0]);
                p2 = new Pos(xbuff[1], ybuff[1]);
            }
            else
            {
                p1 = new Pos();
                p2 = new Pos();
            }
            return new MapField(p1, p2);
        }
        /// <summary>
        /// 座標をセットする
        /// <para>引数で渡された座標を検査して、領域を増やすことができれば拡張が実行されます。</para>
        /// </summary>
        /// <param name="pos">追加・検査する座標</param>
        /// <param name="param">領域のパラメータ（参照渡し）</param>
        private static void SetPos(Pos pos, ref FieldParameter param)
        {
            if (param.upperY > pos.y) param.upperY = pos.y;
            if (param.lowerY < pos.y) param.lowerY = pos.y;
            if (param.leftX > pos.x) param.leftX = pos.x;
            if (param.rightX < pos.y) param.rightX = pos.x;
            return;
        }
        /// <summary>
        /// 座標をセットして領域を再設定・拡張する
        /// <para>領域の中の座標を設定しても無視します。</para>
        /// </summary>
        /// <param name="pos">指定座標</param>
        public void Set(Pos pos)
        {
            SetPos(pos, ref this.param);
            return;
        }
        /// <summary>
        /// 2つの座標をセットするコンストラクタ
        /// <para>指定した座標の大小関係からマップの座標を自動的に決定します。</para>
        /// </summary>
        /// <param name="x1">座標1</param>
        /// <param name="x2">座標2</param>
        public MapField(Pos x1, Pos x2)
        {
            this.param = new FieldParameter();
            this.param.leftX = this.param.rightX = x1.x;        // 座標をセットしておく
            this.param.upperY = this.param.lowerY = x1.y;
            SetPos(x2, ref this.param);                         // もう一点は判定ののちに追加する
            return;
        }
        /// <summary>
        /// 1つの座標をセットするコンストラクタ
        /// <para>自動的にPos(0, 0)をもう一点として張ります。</para>
        /// <para>マップのサイズを表すのに便利かと思います。</para>
        /// </summary>
        public MapField(Pos pos)
        {
            this.param = new FieldParameter();
            SetPos(pos, ref this.param);
            return;
        }
    }
}