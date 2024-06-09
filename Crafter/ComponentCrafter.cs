using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
    public class ComponentCrafter : ComponentInventoryBase
    {
        public int RemainsSlotIndex => SlotsCount - 1;
        public int ResultSlotIndex => SlotsCount - 2;
        public int SlotStates => slotStates;
        public int GirdSlotsCount => SlotsCount - 2;
        //占用0~8位，1表示可用，0表示没用
        private int slotStates;
        public ComponentBlockEntity m_componentBlockEntity;
        public SubsystemPickables m_subsystemPickables;
        public SubsystemProjectiles m_subsystemProjectiles;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemAudio m_subsystemAudio;
        public int m_craftingGridSize;
        public string[] m_matchedIngredients = new string[9];
        public CraftingRecipe m_matchedRecipe;

        public override int GetSlotCapacity(int slotIndex, int value)
        {
            //格子被锁定，无法放置方块
            if (!GetSlotState(slotIndex)) return 0;

            //套组优先
            int maxNum = 0;
            List<int> numsInSlot = new List<int>();
            for (int j = 0; j < GirdSlotsCount; j++)
            {
                if (!GetSlotState(j)) continue;//格子不可用，继续
                maxNum = MathUtils.Max(maxNum, GetSlotCount(j));
                numsInSlot.Add(GetSlotCount(j));
            }
            bool allEqual = numsInSlot.All(x => x == maxNum);//形成了一个套组
            if (allEqual) return base.GetSlotCapacity(slotIndex, value);
            if (GetSlotCount(slotIndex) < maxNum) return base.GetSlotCapacity(slotIndex, value);

            return 0;
            //return base.GetSlotCapacity(slotIndex, value);
        }
        public override void AddSlotItems(int slotIndex, int value, int count)
        {
            base.AddSlotItems(slotIndex, value, count);
            UpdateCraftingResult();
        }
        public override int RemoveSlotItems(int slotIndex, int count)
        {
            int num = 0;
            if (slotIndex == ResultSlotIndex)
            {
                if (m_matchedRecipe != null)
                {
                    if (m_matchedRecipe.RemainsValue != 0 && m_matchedRecipe.RemainsCount > 0)
                    {
                        if (m_slots[RemainsSlotIndex].Count == 0 || m_slots[RemainsSlotIndex].Value == m_matchedRecipe.RemainsValue)
                        {
                            int num2 = BlocksManager.Blocks[Terrain.ExtractContents(m_matchedRecipe.RemainsValue)].GetMaxStacking(m_matchedRecipe.RemainsValue) - m_slots[RemainsSlotIndex].Count;
                            count = MathUtils.Min(count, num2 / m_matchedRecipe.RemainsCount * m_matchedRecipe.ResultCount);
                        }
                        else
                        {
                            count = 0;
                        }
                    }
                    count = count / m_matchedRecipe.ResultCount * m_matchedRecipe.ResultCount;
                    num = base.RemoveSlotItems(slotIndex, count);
                    if (num > 0)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            if (!string.IsNullOrEmpty(m_matchedIngredients[i]))
                            {
                                int index = i % 3 + m_craftingGridSize * (i / 3);
                                m_slots[index].Count = MathUtils.Max(m_slots[index].Count - num / m_matchedRecipe.ResultCount, 0);
                            }
                        }
                        if (m_matchedRecipe.RemainsValue != 0 && m_matchedRecipe.RemainsCount > 0)
                        {
                            m_slots[RemainsSlotIndex].Value = m_matchedRecipe.RemainsValue;
                            m_slots[RemainsSlotIndex].Count += num / m_matchedRecipe.ResultCount * m_matchedRecipe.RemainsCount;
                        }
                        ComponentPlayer componentPlayer = FindInteractingPlayer();
                        if (componentPlayer != null && componentPlayer.PlayerStats != null)
                        {
                            componentPlayer.PlayerStats.ItemsCrafted += num;
                        }
                    }
                }
            }
            else
            {
                num = base.RemoveSlotItems(slotIndex, count);
            }
            UpdateCraftingResult();
            return num;
        }
        //在按钮改变状态时执行
        public void OnSlotLockStateChanged(int index, bool isLocked)
        {
            SetSlotState(index, isLocked);
            if (isLocked) DisposeItemsInSlot(index);//如果锁定了，丢掉格子内所有方块
            UpdateCraftingResult();
        }
        private void SetSlotState(int index, bool isLocked)
        {
            int mask = 1 << index; // 计算第index位的掩码
            slotStates = !isLocked ? (slotStates | mask) : (slotStates & ~mask);
        }
        public bool GetSlotState(int index)
        {
            // 使用位运算符&检查第index位是否等于1
            int mask = 1 << index;
            int result = slotStates & mask;

            // 如果第index位等于1，返回true；否则返回false
            return result != 0;
        }
        private void DisposeItemsInSlot(int index)
        {
            Point3 coordinates = m_componentBlockEntity.Coordinates;
            int data = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z));
            int direction = DispenserBlock.GetDirection(data);
            DispenserBlock.Mode mode = DispenserBlock.Mode.Dispense;

            int slotValue = GetSlotValue(index);
            int slotCount = GetSlotCount(index);

            if (slotCount > 0 && slotValue > 0)
            {
                DispenseItem(coordinates, direction, slotCount, slotValue, mode);
                m_slots[index].Count -= slotCount;
            }
        }
        public void DisposeResults(DispenserBlock.Mode mode)
        {
            Point3 coordinates = m_componentBlockEntity.Coordinates;
            int data = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z));
            int direction = DispenserBlock.GetDirection(data);

            int resultValue = GetSlotValue(ResultSlotIndex);
            int resultCount = GetSlotCount(ResultSlotIndex);
            int remainValue = GetSlotValue(RemainsSlotIndex);
            int remainCount = GetSlotCount(RemainsSlotIndex);

            //吐出产物的主次要顺序不能换
            //吐出次要产物
            if (remainCount > 0 && remainValue > 0)
            {
                int recipeRemainCount = m_matchedRecipe == null ? 1 : m_matchedRecipe.RemainsCount;//防止在无配方时弹出副产物时报错.无配方时，发射副产物数量为1
                int count = mode == DispenserBlock.Mode.Dispense ? remainCount : recipeRemainCount;
                DispenseItem(coordinates, direction, count, remainValue, mode);
                RemoveSlotItems(RemainsSlotIndex, count);
            }

            //吐出主要产物
            if (resultCount > 0 && resultValue > 0)
            {
                int count = mode == DispenserBlock.Mode.Dispense ? resultCount : m_matchedRecipe.ResultCount;
                DispenseItem(coordinates, direction, count, resultValue, mode);
                RemoveSlotItems(ResultSlotIndex, count);
            }

            UpdateCraftingResult();
        }


        //投掷物体
        public virtual void DispenseItem(Point3 point, int face, int count, int value, DispenserBlock.Mode mode)
        {
            Vector3 vector = CellFace.FaceToVector3(face);
            Vector3 position = new Vector3(point.X + 0.5f, point.Y + 0.5f, point.Z + 0.5f) + 0.6f * vector;
            if (mode == DispenserBlock.Mode.Dispense)
            {
                float s = 1.8f;
                m_subsystemPickables.AddPickable(value, count, position, s * (vector + m_random.Vector3(0.2f)), null);
                m_subsystemAudio.PlaySound("Audio/DispenserDispense", 1f, 0f, new Vector3(position.X, position.Y, position.Z), 3f, autoDelay: true);
                return;
            }
            float s2 = m_random.Float(39f, 41f);
            for (int i = 0; i < count; i++)
            {
                if (m_subsystemProjectiles.FireProjectile(value, position, s2 * (vector + m_random.Vector3(0.025f) + new Vector3(0f, 0.05f, 0f)), Vector3.Zero, null) != null)
                {
                    m_subsystemAudio.PlaySound("Audio/DispenserShoot", 1f, 0f, new Vector3(position.X, position.Y, position.Z), 4f, autoDelay: true);
                }
                else
                {
                    DispenseItem(point, face, 1, value, DispenserBlock.Mode.Dispense);
                }
            }
        }
        //匹配配方
        public virtual void UpdateCraftingResult()
        {
            int num = int.MaxValue;
            for (int i = 0; i < m_craftingGridSize; i++)
            {
                for (int j = 0; j < m_craftingGridSize; j++)
                {
                    int num2 = i + j * 3;
                    int slotIndex = i + j * m_craftingGridSize;
                    int slotValue = GetSlotValue(slotIndex);
                    int num3 = Terrain.ExtractContents(slotValue);
                    int num4 = Terrain.ExtractData(slotValue);
                    int slotCount = GetSlotCount(slotIndex);
                    if (slotCount > 0)
                    {
                        Block block = BlocksManager.Blocks[num3];
                        m_matchedIngredients[num2] = block.GetCraftingId(slotValue) + ":" + num4.ToString(CultureInfo.InvariantCulture);
                        num = MathUtils.Min(num, slotCount);
                    }
                    else
                    {
                        m_matchedIngredients[num2] = null;
                    }
                }
            }
            ComponentPlayer componentPlayer = FindInteractingPlayer();
            float playerLevel = componentPlayer?.PlayerData.Level ?? 1f;
            CraftingRecipe craftingRecipe = CraftingRecipesManager.FindMatchingRecipe(Project.FindSubsystem<SubsystemTerrain>(throwOnError: true), m_matchedIngredients, 0f, playerLevel);
            if (craftingRecipe != null && craftingRecipe.ResultValue != 0)
            {
                m_matchedRecipe = craftingRecipe;
                m_slots[ResultSlotIndex].Value = craftingRecipe.ResultValue;
                m_slots[ResultSlotIndex].Count = craftingRecipe.ResultCount * num;
            }
            else
            {
                m_matchedRecipe = null;
                m_slots[ResultSlotIndex].Value = 0;
                m_slots[ResultSlotIndex].Count = 0;
            }
            if (craftingRecipe != null && !string.IsNullOrEmpty(craftingRecipe.Message))
            {
                string message = craftingRecipe.Message;
                if (message.StartsWith("[") && message.EndsWith("]"))
                {
                    message = LanguageControl.Get("CRMessage", message.Substring(1, message.Length - 2));
                }
                componentPlayer?.ComponentGui.DisplaySmallMessage(message, Color.White, blinking: true, playNotificationSound: true);
            }
        }


        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
        {
            base.Load(valuesDictionary, idToEntityMap);
            slotStates = valuesDictionary.GetValue<int>("SlotStates");
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
            m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
            m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
            m_craftingGridSize = (int)MathUtils.Sqrt(SlotsCount - 2);

            //List<int> ids = subsystemElectricity.
        }
        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
        {
            base.Save(valuesDictionary, entityToIdMap);
            valuesDictionary.SetValue("SlotStates", slotStates);
        }


        /*
            public int m_craftingGridSize;

            public string[] m_matchedIngredients = new string[9];

            public CraftingRecipe m_matchedRecipe;

            public int RemainsSlotIndex => SlotsCount - 1;

            public int ResultSlotIndex => SlotsCount - 2;

            public override int GetSlotCapacity(int slotIndex, int value)
            {
                if (slotIndex < SlotsCount - 2)
                {
                    return base.GetSlotCapacity(slotIndex, value);
                }
                return 0;
            }

            public override void AddSlotItems(int slotIndex, int value, int count)
            {
                base.AddSlotItems(slotIndex, value, count);
                UpdateCraftingResult();
            }

            public override int RemoveSlotItems(int slotIndex, int count)
            {
                int num = 0;
                if (slotIndex == ResultSlotIndex)
                {
                    if (m_matchedRecipe != null)
                    {
                        if (m_matchedRecipe.RemainsValue != 0 && m_matchedRecipe.RemainsCount > 0)
                        {
                            if (m_slots[RemainsSlotIndex].Count == 0 || m_slots[RemainsSlotIndex].Value == m_matchedRecipe.RemainsValue)
                            {
                                int num2 = BlocksManager.Blocks[Terrain.ExtractContents(m_matchedRecipe.RemainsValue)].GetMaxStacking(m_matchedRecipe.RemainsValue) - m_slots[RemainsSlotIndex].Count;
                                count = MathUtils.Min(count, num2 / m_matchedRecipe.RemainsCount * m_matchedRecipe.ResultCount);
                            }
                            else
                            {
                                count = 0;
                            }
                        }
                        count = count / m_matchedRecipe.ResultCount * m_matchedRecipe.ResultCount;
                        num = base.RemoveSlotItems(slotIndex, count);
                        if (num > 0)
                        {
                            for (int i = 0; i < 9; i++)
                            {
                                if (!string.IsNullOrEmpty(m_matchedIngredients[i]))
                                {
                                    int index = i % 3 + m_craftingGridSize * (i / 3);
                                    m_slots[index].Count = MathUtils.Max(m_slots[index].Count - num / m_matchedRecipe.ResultCount, 0);
                                }
                            }
                            if (m_matchedRecipe.RemainsValue != 0 && m_matchedRecipe.RemainsCount > 0)
                            {
                                m_slots[RemainsSlotIndex].Value = m_matchedRecipe.RemainsValue;
                                m_slots[RemainsSlotIndex].Count += num / m_matchedRecipe.ResultCount * m_matchedRecipe.RemainsCount;
                            }
                            ComponentPlayer componentPlayer = FindInteractingPlayer();
                            if (componentPlayer != null && componentPlayer.PlayerStats != null)
                            {
                                componentPlayer.PlayerStats.ItemsCrafted += num;
                            }
                        }
                    }
                }
                else
                {
                    num = base.RemoveSlotItems(slotIndex, count);
                }
                UpdateCraftingResult();
                return num;
            }

            public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
            {
                base.Load(valuesDictionary, idToEntityMap);
                m_craftingGridSize = (int)MathUtils.Sqrt(SlotsCount - 2);
                UpdateCraftingResult();
            }

            public virtual void UpdateCraftingResult()
            {
                int num = int.MaxValue;
                for (int i = 0; i < m_craftingGridSize; i++)
                {
                    for (int j = 0; j < m_craftingGridSize; j++)
                    {
                        int num2 = i + j * 3;
                        int slotIndex = i + j * m_craftingGridSize;
                        int slotValue = GetSlotValue(slotIndex);
                        int num3 = Terrain.ExtractContents(slotValue);
                        int num4 = Terrain.ExtractData(slotValue);
                        int slotCount = GetSlotCount(slotIndex);
                        if (slotCount > 0)
                        {
                            Block block = BlocksManager.Blocks[num3];
                            m_matchedIngredients[num2] = block.GetCraftingId(slotValue) + ":" + num4.ToString(CultureInfo.InvariantCulture);
                            num = MathUtils.Min(num, slotCount);
                        }
                        else
                        {
                            m_matchedIngredients[num2] = null;
                        }
                    }
                }
                ComponentPlayer componentPlayer = FindInteractingPlayer();
                float playerLevel = componentPlayer?.PlayerData.Level ?? 1f;
                CraftingRecipe craftingRecipe = CraftingRecipesManager.FindMatchingRecipe(Project.FindSubsystem<SubsystemTerrain>(throwOnError: true), m_matchedIngredients, 0f, playerLevel);
                if (craftingRecipe != null && craftingRecipe.ResultValue != 0)
                {
                    m_matchedRecipe = craftingRecipe;
                    m_slots[ResultSlotIndex].Value = craftingRecipe.ResultValue;
                    m_slots[ResultSlotIndex].Count = craftingRecipe.ResultCount * num;
                }
                else
                {
                    m_matchedRecipe = null;
                    m_slots[ResultSlotIndex].Value = 0;
                    m_slots[ResultSlotIndex].Count = 0;
                }
                if (craftingRecipe != null && !string.IsNullOrEmpty(craftingRecipe.Message))
                {
                    string message = craftingRecipe.Message;
                    if (message.StartsWith("[") && message.EndsWith("]"))
                    {
                        message = LanguageControl.Get("CRMessage", message.Substring(1, message.Length - 2));
                    }
                    componentPlayer?.ComponentGui.DisplaySmallMessage(message, Color.White, blinking: true, playNotificationSound: true);
                }
            }
            */
    }
}
