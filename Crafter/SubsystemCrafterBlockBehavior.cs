using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
    public class SubsystemCrafterBlockBehavior : SubsystemBlockBehavior
    {
        public SubsystemBlockEntities m_subsystemBlockEntities;

        public SubsystemAudio m_subsystemAudio;
        public override int[] HandledBlocks => new int[1]
        {
            545
        };
        public override void Load(ValuesDictionary valuesDictionary)
        {
            base.Load(valuesDictionary);
            m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
        {
            DatabaseObject databaseObject = SubsystemTerrain.Project.GameDatabase.Database.FindDatabaseObject("Crafter", SubsystemTerrain.Project.GameDatabase.EntityTemplateType, throwIfNotFound: true);
            var valuesDictionary = new ValuesDictionary();
            valuesDictionary.PopulateFromDatabaseObject(databaseObject);
            valuesDictionary.GetValue<ValuesDictionary>("BlockEntity").SetValue("Coordinates", new Point3(x, y, z));
            Entity entity = SubsystemTerrain.Project.CreateEntity(valuesDictionary);
            SubsystemTerrain.Project.AddEntity(entity);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
        {
            ComponentBlockEntity blockEntity = SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(x, y, z);
            if (blockEntity != null)
            {
                Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
                foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
                {
                    item.DropAllItems(position);
                }
                SubsystemTerrain.Project.RemoveEntity(blockEntity.Entity, disposeEntity: true);
            }
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
        {
            ComponentBlockEntity blockEntity = SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
            if (blockEntity != null && componentMiner.ComponentPlayer != null)
            {
                ComponentCrafter componentCraftingTable = blockEntity.Entity.FindComponent<ComponentCrafter>(throwOnError: true);
                componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new CrafterWidget(componentMiner.Inventory, componentCraftingTable, componentMiner.ComponentPlayer);
                AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
                return true;
            }
            return false;
        }

        public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
        {
            if (worldItem.ToRemove)
            {
                return;
            }
            ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(cellFace.X, cellFace.Y, cellFace.Z);
            if (blockEntity != null)
            {
                ComponentCrafter inventory = blockEntity.Entity.FindComponent<ComponentCrafter>(throwOnError: true);
                var pickable = worldItem as Pickable;
                int num = pickable?.Count ?? 1;
                int num2 = ComponentInventoryBase.AcquireItems(inventory, worldItem.Value, num);
                if (num2 < num)
                {
                    m_subsystemAudio.PlaySound("Audio/PickableCollected", 1f, 0f, worldItem.Position, 3f, autoDelay: true);
                }
                if (num2 <= 0)
                {
                    worldItem.ToRemove = true;
                }
                else if (pickable != null)
                {
                    pickable.Count = num2;
                }
            }
        }
    }
}
