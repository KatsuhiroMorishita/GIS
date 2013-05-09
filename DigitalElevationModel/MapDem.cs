/*****************************************************
 * MapDem.cs
 * DEM（デジタル標高モデル）を地図クラスを利用して扱うクラス
 * 
 * マップの取り扱い注意
 * 1)本クラスのコンストラクタ生成時には必ずマップの右上と左下の座標（緯度・経度）をセットして下さい。
 * 
 * 　開発者：K.Morishita (Kumamoto-University Japan @2012)
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
 * 　        2012/5/4    クラス内部に宣言している構造体を別ファイルに移した。
 * 　        2012/5/28   Map.NAを廃止し、該当データが存在しない場合にはfloat.NaNを返すように仕様を変更した。
 * 　                    DEMクラスのマップ生成アルゴリズムの大幅な変更に伴い利用しなくなったメソッドを削除した。
 * 　        2012/5/29   SaveAsImage()を高速化
 * 　        2012/6/2    メッシュ数を表すプロパティ名がLenghになっていたのをLengthと訂正した。
 * 　        2012/6/4    コピーコンストラクタを新設した。
 * 　                    これでマップのインスタンスを安全にコピーできるようになった。
 * 　        2012/6/8    一部のコメントがコードと一致していなかったため変更した。
 * 　                    また、メソッド名に使っていたXMLをXmlへと変更した。
 * 　        2012/7/12   XYaddress2Position()をpublicへ変更した。
 * 　                    アドレス変換メソッド二つをインターフェイスメンバとするIAddressChangerを新設して、継承するように変更した。
 * 　                    将来的にはアドレス変換専用クラスを作るようなメソッドを新設するかもしれないけど、移行はしやすいと思う。
 * 　        2012/8/1    コメント一部見直し
 * 　        2012/9/2    マップから任意の範囲の地図を切り出すCropMap()を整備。
 * 　        2012/9/4    傾斜地の情報を出力できるように、GetSlopeMap()を整備。
 * 　        2012/9/29   MapScalarクラスを継承する形へ変更した。
 * 　        2012/9/30   MapクラスからMapDemクラスへ名前を変更した。
 * 　                    名前空間もDigitalElevationModelへ変更した。
 * 　                    現時点でのMapBase名前空間内のMapクラスは、ジェネリック化されて汎化されている。
 * 　                    本クラスは、Mapクラスから派生させたMapScalarクラスを継承させる形に改めている。
 * 　                    今後はDEM固有の処理を入れてもいいかもしれない。
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
using GIS.MapBase;

namespace GIS.DigitalElevationModel
{

    /// <summary>
    /// DEM（デジタル標高モデル）を地図クラスを利用して扱うクラス
    /// <para>
    /// 内部では要素を2次元配列として表現しています。
    /// 配列は[x,y]で宣言しており、画像でたとえると[0,0]が画面の左上、[x_max, y_max]が画面の右下としています。
    /// 画面の上が北で、右が東です。
    /// 東西方向をx、南北方向をyで表現しています。
    /// xは増えるほどより東に位置することを表し、yは増えるほどより南に位置することを表します。
    /// </para>
    /// </summary>
    public class MapDem : MapScalar<float>
    {
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
        public MapDem(RectangleField field)
            : base(field)
        {
            return;
        }
        /// <summary>
        /// 領域の角の座標とマップのサイズ（ピクセル数）を指定することでマップを生成するコンストラクタ
        /// <para>
        /// マップは0で初期化されます。
        /// </para>
        /// </summary>
        /// <param name="field">マップの領域情報</param>
        /// <param name="_size">縦横のマップサイズ</param>
        public MapDem(RectangleField field, Pos _size)
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
        public MapDem(RectangleField field, Length _meshSize)
            : base(field, _meshSize)
        {
            return;
        }
        /// <summary>
        /// コピーコンストラクタ
        /// <para>パレメータを始め全てのパラメータをコピーしたインスタンスを生成します。</para>
        /// </summary>
        /// <param name="map">コピー元のマップ</param>
        public MapDem(MapDem map)
            : base(map)
        {
            return;
        }
        /// <summary>
        /// デスコンストラクタ
        /// </summary>
        ~MapDem()
        {
            this.d_map = null;
        }
        #endregion
    }
}
