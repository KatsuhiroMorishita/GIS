/****************************************************************************
 * DemSet.cs
 * DEMファイル情報とマップを結合し、運用するためのクラス
 * 
 * [課題]
 *      1) 
 * 
 * [history]
 *      2012/5/22   マップをロード済みかどうかを返すプロパティを新設
 *                  データをヌル許容型とするために構造体ではなくクラスへ変更した。
 *      2012/5/24   マップの読み込みを実施するLoad()とToString()を実装した。
 * **************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GIS.MapBase;
using GIS.DigitalElevationModel.Reader;

namespace GIS.DigitalElevationModel
{
    /// <summary>
    /// DEMファイル情報とマップを結合し、運用するためのクラス
    /// <para>マップ情報とマップクラスとを一緒にする。</para>
    /// </summary>
    public class DemSet
    {
        /// <summary>
        /// マップ情報
        /// </summary>
        private Info info;
        /// <summary>
        /// 数値標高データを格納・管理するマップ
        /// </summary>
        public MapDem map;
        /* プロパティ **************************************************/
        /// <summary>
        /// 数値標高モデルを格納したマップ
        /// </summary>
        public MapDem DemMap
        {
            get 
            {
                return this.map;
            }
        }
        /// <summary>
        /// DEMの情報
        /// </summary>
        public Info DataInfo
        {
            get { return this.info; }
        }
        /// <summary>
        /// マップをすでに読み込んでいるかどうか
        /// <para>true: ロード済み</para>
        /// </summary>
        public Boolean IsLoaded
        {
            get
            {
                if (this.map == null) 
                    return false;
                else
                    return this.map.IsMapSet;
            }
        }
        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName
        {
            get;
            private set;
        }
        /// <summary>
        /// 保持しているDEM情報とマップの一致性を確認します
        /// <para>セルフテストの一環です。</para>
        /// <para>true: 問題は見られない（矛盾なし）, false: 矛盾あり、若しくは評価不能</para>
        /// </summary>
        public Boolean Identity
        {
            get
            {
                if (this.IsLoaded)
                {
                    if (this.map.Field.LowerLeft == this.info.Field.LowerLeft &&
                        this.map.Field.UpperRight == this.info.Field.UpperRight &&
                        this.map.Size == this.info.Size
                        )
                        return true;
                }
                return false;
            }
        }
        /// <summary>
        /// エラー状況
        /// <para>true: エラーがあります。エラーの詳細はErrorMsgをご確認ください。</para>
        /// </summary>
        public Boolean Error
        {
            get {
                if (this.ErrorMsg == "")
                    return false;
                else
                    return true;
            }
        }
        /// <summary>
        /// エラーメッセージ
        /// <para>何かしらの記述があれば、エラーがあることを示します。</para>
        /// </summary>
        public string ErrorMsg
        {
            get;
            private set;
        }
        /* メソッド **************************************************/
        /// <summary>
        /// 保持しているDEM情報を文字列として返す
        /// </summary>
        /// <returns>DEM情報</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(5000);                      // 予め大きなメモリ容量を確保しておく
            sb.Append(this.FileName).Append(",");
            sb.Append(this.IsLoaded.ToString()).Append(",").Append(this.Identity.ToString()).Append(",");
            string infoText = this.info.ToString();
            infoText = infoText.Replace(Environment.NewLine, "");           // 改行コードを削除
            sb.Append(infoText).Append("\n") ;
            return sb.ToString();
        }
        /// <summary>
        /// DEM数値情報をメモリに読み出します
        /// <para>読み込み済みの場合は無視されます。</para>
        /// </summary>
        /// <returns>読み込みに成功するとtrueを返します。</returns>
        public Boolean Load()
        {
            Boolean result = false;

            if (this.IsLoaded == false)
            {
                Reader.Reader reader = new Reader.Reader();
                this.map = reader.ReadMap(this.FileName, this.info);
                if (map != null)
                    result = true;
                else
                    this.ErrorMsg = "マップの読み込みに失敗しました。ファイルが存在しない・ロックされている・壊れているのいずれかが考えられます。";
            }
            return result;
        }
        /// <summary>
        ///マップのインスタンスを生成しないコンストラクタ
        ///<para>DEMデータ情報について整理し、マップを管理する準備を行います。</para>
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <param name="info">マップ情報</param>
        /// <param name="parentFileName">親ファイル名<para>元ファイルが圧縮ファイル等であった場合に利用してください。</para></param>
        public DemSet(string fileName, Info info, string parentFileName = "")
            : base()
        {
            this.FileName = fileName;
            this.info = info;
            this.map = null;                        // メモリ空間を始めは食わせたくないので、マップのサイズはセットしない。
            this.ErrorMsg = "";
        }
    }
}
