using Engine;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Game
{
    public class CrafterWidget : CanvasWidget
    {
        public GridPanelWidget m_inventoryGrid;

        public GridPanelWidget m_craftingGrid;

        public InventorySlotWidget m_craftingResultSlot;

        public InventorySlotWidget m_craftingRemainsSlot;

        public ComponentCrafter m_componentCraftingTable;

        public ComponentPlayer m_componentPlayer;

        public BitmapButtonWidget helpButton;

        public List<ButtonWidget> girdButtons = new List<ButtonWidget>();

        public CrafterWidget(IInventory inventory, ComponentCrafter componentCraftingTable, ComponentPlayer componentPlayer)
        {
            m_componentCraftingTable = componentCraftingTable;
            m_componentPlayer = componentPlayer;
            XElement node = ContentManager.Get<XElement>("Add/Crafter/CrafterWidget");
            LoadContents(this, node);
            m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
            m_craftingGrid = Children.Find<GridPanelWidget>("CraftingGrid");
            m_craftingResultSlot = Children.Find<InventorySlotWidget>("CraftingResultSlot");
            m_craftingRemainsSlot = Children.Find<InventorySlotWidget>("CraftingRemainsSlot");

            helpButton = Children.Find<BitmapButtonWidget>("HelpButton");
            helpButton.NormalSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/HelpTopicIcon");
            helpButton.ClickedSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/HelpTopicIcon");
            helpButton.Size = new Vector2(36, 36);

            int num = 10;
            //背包
            for (int i = 0; i < m_inventoryGrid.RowsCount; i++)
            {
                for (int j = 0; j < m_inventoryGrid.ColumnsCount; j++)
                {
                    var inventorySlotWidget = new InventorySlotWidget();
                    inventorySlotWidget.AssignInventorySlot(inventory, num++);
                    m_inventoryGrid.Children.Add(inventorySlotWidget);
                    m_inventoryGrid.SetWidgetCell(inventorySlotWidget, new Point2(j, i));
                }
            }
            num = 0;
            //合成九宫格
            for (int k = 0; k < m_craftingGrid.RowsCount; k++)
            {
                for (int l = 0; l < m_craftingGrid.ColumnsCount; l++)
                {
                    var clickableSlot = new ClickableSlotWidget(num, m_componentCraftingTable.OnSlotLockStateChanged);
                    clickableSlot.AssignInventorySlot(m_componentCraftingTable, num);
                    clickableSlot.IsChecked = m_componentCraftingTable.GetSlotState(num);
                    m_craftingGrid.Children.Add(clickableSlot);
                    m_craftingGrid.SetWidgetCell(clickableSlot, new Point2(l, k));
                    num++;
                }
            }
            m_craftingResultSlot.AssignInventorySlot(m_componentCraftingTable, m_componentCraftingTable.ResultSlotIndex);
            m_craftingRemainsSlot.AssignInventorySlot(m_componentCraftingTable, m_componentCraftingTable.RemainsSlotIndex);
        }

        public override void Update()
        {
            if (!m_componentCraftingTable.IsAddedToProject)
            {
                ParentWidget.Children.Remove(this);
            }

            if (helpButton.IsClicked)
            {
                string helpText = "操作步骤:\r\n" +
                    "1.在界面中点击需要的格子，使得它们的状态为可用\r\n" +
                    "2.用发射器或者投抛来装原料\r\n" +
                    "3.用上升沿激活（电压第三位为0：投掷产物，为1：发射产物）\r\n" +
                    "例如: 电压为11，其二进制为1011，第三位为0，为投掷模式；电压为12，其二进制为1100，第三位为1，为发射模式\r\n" +
                    "\r\n" +
                    "特性说明:\r\n" +
                    "1.在九宫格内有空槽时，物品优先填充至空槽中(有物品的槽就放不了物品了)\r\n" +
                    "2.当九宫格内所有可用槽中的物品数量分别相等时，可在槽中放入物品\r\n" +
                    "3.当九宫格内所有可用槽中的物品数量分别不相等时，物品优先填充入数量少的槽\r\n" +
                    "(物品数量多的槽就无法再放入物品了)\r\n" +
                    "4.模式为投掷时，会投掷所有所有产物；模式为发射时，每次只会发射一份产物\r\n" +
                    "(在无配方成立时，发射副产物时每次的发射数量为1)\r\n" +
                    "\r\n" +
                    "注意事项:\r\n" +
                    "若要将九宫格内的原料取出，点击原料所在的槽位使其被禁用，原料即可从合成台发射面投掷出";
                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog("帮助", helpText, "确定", null, new Vector2(700, 400), null));
            }
        }
    }
}
