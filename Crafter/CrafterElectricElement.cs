using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;

namespace Game
{
    public class CrafterElectricElement : ElectricElement
    {
        public bool m_isDispenseAllowed = true;

        public double? m_lastDispenseTime;

        public SubsystemBlockEntities m_subsystemBlockEntities;

        public float m_voltage;

        public CrafterElectricElement(SubsystemElectricity subsystemElectricity, Point3 point)
            : base(subsystemElectricity, new List<CellFace>
            {
                new CellFace(point.X, point.Y, point.Z, 0),
                new CellFace(point.X, point.Y, point.Z, 1),
                new CellFace(point.X, point.Y, point.Z, 2),
                new CellFace(point.X, point.Y, point.Z, 3),
                new CellFace(point.X, point.Y, point.Z, 4),
                new CellFace(point.X, point.Y, point.Z, 5)
            })
        {
            m_subsystemBlockEntities = SubsystemElectricity.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
        }

        public override bool Simulate()
        {
            if (CalculateHighInputsCount() > 0)
            {
                m_voltage = 0f;
                foreach (ElectricConnection connection in Connections)
                {
                    if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != 0)
                    {
                        m_voltage = MathUtils.Max(m_voltage, connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                    }
                }

                if (m_isDispenseAllowed && (!m_lastDispenseTime.HasValue || SubsystemElectricity.SubsystemTime.GameTime - m_lastDispenseTime > 0.1))
                {
                    m_isDispenseAllowed = false;
                    m_lastDispenseTime = SubsystemElectricity.SubsystemTime.GameTime;

                    int voltage = (int)MathUtils.Round(m_voltage * 15f);

                    DispenserBlock.Mode mode = ((voltage >> 2) & 1) == 1 ? DispenserBlock.Mode.Shoot : DispenserBlock.Mode.Dispense;
                    m_subsystemBlockEntities.GetBlockEntity(CellFaces[0].Point.X, CellFaces[0].Point.Y, CellFaces[0].Point.Z)?.Entity.FindComponent<ComponentCrafter>()?.DisposeResults(mode);
                }
            }
            else
            {
                m_isDispenseAllowed = true;
            }
            return false;
        }
    }
}