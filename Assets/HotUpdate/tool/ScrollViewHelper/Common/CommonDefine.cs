using System;
using System.Collections.Generic;

namespace SuperScrollView
{

    public enum SnapStatus
    {
        NoTargetSet = 0,
        TargetHasSet = 1,
        SnapMoving = 2,
        SnapMoveFinish = 3
    }


    public enum ItemCornerEnum
    {
        LeftBottom = 0,
        LeftTop,
        RightTop,
        RightBottom,
    }


    public enum ListItemArrangeType
    {
        TopToBottom = 0,
        BottomToTop,
        LeftToRight,
        RightToLeft,
        MiddleCenter,
    }

    public enum GridItemArrangeType
    {
        TopLeftToBottomRight = 0,
        BottomLeftToTopRight,
        TopRightToBottomLeft,
        BottomRightToTopLeft,
    }
    public enum GridFixedType
    {
        ColumnCountFixed = 0,
        RowCountFixed,
    }

    public struct RowColumnPair
    {
        public RowColumnPair(int row1, int column1)
        {
            mRow = row1;
            mColumn = column1;
        }

        public bool Equals(RowColumnPair other)
        {
            return this.mRow == other.mRow && this.mColumn == other.mColumn;
        }

        public static bool operator ==(RowColumnPair a, RowColumnPair b)
        {
            return (a.mRow == b.mRow) && (a.mColumn == b.mColumn);
        }
        public static bool operator !=(RowColumnPair a, RowColumnPair b)
        {
            return (a.mRow != b.mRow) || (a.mColumn != b.mColumn); ;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return (obj is RowColumnPair) && Equals((RowColumnPair)obj);
        }


        public int mRow;
        public int mColumn;
    }
}
/// <summary>
/// 精灵资源路径定义
/// 需要根文件夹同名 图集和散图 都可以使用
/// </summary>
public static class ResPathSprite
{
    public const string Album = "Album/";
    public const string Backpack = "Backpack/";
    public const string Battle = "Battle/";
    public const string Common = "Common/";
    public const string Equipment = "Equipment/";
    public const string Hero = "Hero/";
    public const string Icon = "Icon/";
    public const string Shop = "Shop/";
    public const string Item = "Item/";
    public const string Login = "Login/"; // 登录界面
    public const string Quality = "Quality/";

    public const string Function = "Function/";
    public const string CampBonus = "CampBonus/";
    public const string MainLevel = "MainLevel/";
}

/// <summary>
/// 纹理资源路径定义
/// </summary>
public static class ResPathTexture
{
    public const string Bg = "Bg/"; // 登录界面
    public const string Common = "Common/"; // 通用纹理

    public const string Headshot = "Sprite/Icon/Headshot/"; // 玩家头像
    public const string HeroDraw = "Texture/HeroDraw/"; // 玩家全身像



}