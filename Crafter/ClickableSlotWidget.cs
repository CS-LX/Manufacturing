using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;

namespace Game
{
    public class ClickableSlotWidget : InventorySlotWidget
    {
#if true
        /// <summary>
        /// 内置的按钮
        /// </summary>
        public ButtonWidget InnerButton => innerButton;
        public BlockIconWidget InnerBlockIcon => innerBlockIcon;
        public bool IsChecked { get; set; }
        public bool IsLocked => !IsChecked;
        public Action<int, bool> OnLockStateChange { get; set; }

        private BevelledButtonWidget innerButton;
        private BlockIconWidget innerBlockIcon;
        private Color buttonNormalColor;
        public int index;

        public ClickableSlotWidget(int index, Action<int, bool> onLockStateChange) : base()
        {
            this.index = index;
            OnLockStateChange = onLockStateChange;

            //声明按钮
            BevelledButtonWidget innerButton = new BevelledButtonWidget();
            innerButton.IsAutoCheckingEnabled = true;
            this.innerButton = innerButton;
            Children.Add(innerButton);
            buttonNormalColor = innerButton.CenterColor;

            //添加BlockIcon
            BlockIconWidget blockIconWidget = new BlockIconWidget();
            this.innerBlockIcon = blockIconWidget;
            blockIconWidget.DrawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
            blockIconWidget.Name = "Icon";
            blockIconWidget.Margin = new Engine.Vector2(12, 12);
            innerButton.AddChildren(blockIconWidget);
        }

        public override void Update()
        {
            base.Update();
            SetUIState();

            //获取方块
            int slotValue = m_inventory.GetSlotValue(m_slotIndex);
            int num = Terrain.ExtractContents(slotValue);
            Block block = BlocksManager.Blocks[num];
            //设置属性
            if (m_componentPlayer != null)
            {
                innerBlockIcon.DrawBlockEnvironmentData.InWorldMatrix = m_componentPlayer.ComponentBody.Matrix;
            }
            innerBlockIcon.Value = Terrain.ReplaceLight(slotValue, 15);

            //按钮
            if(innerButton.IsClicked)
            {
                IsChecked = !IsChecked;
                OnLockStateChange(index, IsLocked);//执行委托
            }
            innerButton.IsChecked = IsChecked;
        }

        private void SetUIState()
        {
            HideBlockIcon = true;//隐藏原有的Icon
            HideEditOverlay = IsLocked;
            HideFoodOverlay = IsLocked;
            HideHealthBar = IsLocked;
            HideHighlightRectangle = IsLocked;
            HideInteractiveOverlay = IsLocked;
            innerButton.CenterColor = IsLocked ? buttonNormalColor : new Color(0,0,0,0);
        }
#endif
    }
}
