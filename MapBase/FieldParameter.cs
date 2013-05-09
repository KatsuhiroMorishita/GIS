using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GIS.MapBase
{
    /// <summary>
    /// MapFieldクラスのパラメータ
    /// </summary>
    internal struct FieldParameter
    {
        /** パラメータ ********************************************************************/
        /// <summary>
        /// 上端y座標
        /// </summary>
        public int upperY;
        /// <summary>
        /// 下端y座標
        /// </summary>
        public int lowerY;
        /// <summary>
        /// 右端x座標
        /// </summary>
        public int rightX;
        /// <summary>
        /// 左端x座標
        /// </summary>
        public int leftX;
        /** プロパティ ********************************************************************/
        /// <summary>
        /// 面積が存在するかを返す
        /// <para>true: 面積は0。</para>
        /// </summary>
        public Boolean AreaIsZero
        {
            get
            {
                // 座標が同じであれば面積0とみなす
                if (this.upperY == this.lowerY || this.rightX == this.leftX)
                    return true;
                else
                    return false;
            }
        }
    }
}
