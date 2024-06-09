using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Engine.Graphics;
using Game;

namespace Game
{
    public class CrafterBlock : CubeBlock, IElectricElementBlock
    {
        public const int Index = 545;
        public Texture2D texture;

        public override void Initialize()
        {
            base.Initialize();
            texture = ContentManager.Get<Texture2D>("Add/Crafter/Blocks");
        }
        public override int GetFaceTextureSlot(int face, int value)
        {
            switch (face)
            {
                case 4:
                    return 43;
                case 5:
                    return 43;
                default:
                    switch (Terrain.ExtractData(value))
                    {
                        case 0:
                            switch (face)
                            {
                                case 0:
                                    return 27 + 32;
                                case 2:
                                    return 26;
                                default:
                                    return 25;
                            }
                        case 1:
                            switch (face)
                            {
                                case 1:
                                    return 27 + 32;
                                case 3:
                                    return 26;
                                default:
                                    return 25;
                            }
                        case 2:
                            switch (face)
                            {
                                case 2:
                                    return 27 + 32;
                                case 0:
                                    return 26;
                                default:
                                    return 25;
                            }
                        default:
                            switch (face)
                            {
                                case 3:
                                    return 27 + 32;
                                case 1:
                                    return 26;
                                default:
                                    return 25;
                            }
                    }
            }
        }
        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
        {
            Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
            float num = Vector3.Dot(forward, Vector3.UnitZ);
            float num2 = Vector3.Dot(forward, Vector3.UnitX);
            float num3 = Vector3.Dot(forward, -Vector3.UnitZ);
            float num4 = Vector3.Dot(forward, -Vector3.UnitX);
            int data = 0;
            if (num == MathUtils.Max(num, num2, num3, num4))
            {
                data = 2;
            }
            else if (num2 == MathUtils.Max(num, num2, num3, num4))
            {
                data = 3;
            }
            else if (num3 == MathUtils.Max(num, num2, num3, num4))
            {
                data = 0;
            }
            else if (num4 == MathUtils.Max(num, num2, num3, num4))
            {
                data = 1;
            }
            BlockPlacementData result = default;
            result.Value = Terrain.ReplaceData(Terrain.ReplaceContents(0, 545), data);
            result.CellFace = raycastResult.CellFace;
            return result;
        }
        public override int GetTextureSlotCount(int value)
        {
            return 16;
        }
        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
        {
            return new CrafterElectricElement(subsystemElectricity, new Point3(x, y, z));
        }
        public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
        {
            return ElectricConnectorType.Input;
        }
        public int GetConnectionMask(int value)
        {
            return int.MaxValue;
        }
        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
        {
            generator.GenerateCubeVertices(this, value, x, y, z, Color.White, geometry.GetGeometry(texture).OpaqueSubsetsByFace);
        }
        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
        {
            BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, color, environmentData, texture);
        }
        public override string GetDescription(int value)
        {
            string description = "你可以用它手动合成物品，可用利用电信号让其自动合成物品。" +
                "它的正面是一个发射口，收到信号后，产物会从其正面发射出。" +
                "其六个面都可以接受掉落物和电信号。" +
                "在它的UI界面里查看详细教程。";
            return description;
        }
    }
}

