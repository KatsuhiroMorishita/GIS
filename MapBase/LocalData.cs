/*****************************************************
 * LocalData.cs
 * とある座標のデータを表す、座標と値のセットの構造体です。
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
 * 　                    ----------------------------------------------------
 * 　        2012/5/4    MapBase.csから移した。
 * 　        2012/5/12   名前空間としてMapBaseを策定したのに伴い、Mapクラスの内部定義からMapBase名前空間直下へ移す。
 * 　        2012/5/17   （この日か、その前日）ジェネリック対応とした。これで任意の座標系を利用することができ、余計な混乱を生じないものとなった。
 * ***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GNSS;

namespace GIS.MapBase
{
    /// <summary>
    /// とある座標のデータを表す、座標と値のセットの構造体
    /// <para>ジェネリックを利用して任意の座標系を表現します。</para>
    /// </summary>
    /// <typeparam name="T">アドレスを表現可能なGNSS.BlhもしくはGIS.MapBase.Posもしくは任意の構造体を指定してください。</typeparam>
    public struct LocalData<T> where T: struct
    {
        /* メンバ変数 ****************************************/
        /* プロパティ ****************************************/
        /// <summary>
        /// 座標
        /// </summary>
        public T Address
        {
            get;
            set;
        }
        /// <summary>
        /// 値
        /// </summary>
        public float Value
        {
            get;
            set;
        }
        /* メソッド ****************************************/
        /// <summary>
        /// 局所情報の文字列化
        /// </summary>
        /// <returns>文字列化したLocalData</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(100);
            sb.Append(this.Address.ToString()).Append(",").Append(this.Value.ToString("0.0"));
            return sb.ToString();
        }
        /* コンストラクタ関係 -----------------------------------*/
        /// <summary>
        /// コンストラクタによる初期化（引数のvalueについては省略可能）
        /// </summary>
        /// <param name="pos">緯度・経度の座標</param>
        /// <param name="value">値</param>
        public LocalData(T pos, float value = float.NaN) : this()
        {
            this.Address = pos;
            this.Value = value;
        }
    }
}
