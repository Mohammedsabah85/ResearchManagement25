using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Domain.Enums
{
    public enum ResearchTrack
    {
        [Display(Name = "غير محدد")]
        NotAssigned = 0,
        [Display(Name = "Energy and Renewable Energy")]
        EnergyAndRenewableEnergy = 1,

        [Display(Name = "Electromechanical System, and Mechatronics Engineering")]
        ElectromechanicalSystemAndMechanicsEngineering = 2,

        [Display(Name = "Material Science & Mechanical Engineering")]
        MaterialScienceAndMechanicalEngineering = 3,

        [Display(Name = "Navigation & Guidance Systems, Computer and Communication Engineering")]
        NavigationGuidanceSystemsComputerAndCommunicationEngineering = 4,

        [Display(Name = "Electrical & Electronics Engineering")]
        ElectricalAndElectronicsEngineering = 5,

        [Display(Name = "Avionics Systems, Aircraft and Unmanned Aircraft Engineering")]
        AvionicsSystemsAircraftAndUnmannedAircraftEngineering = 6,

        [Display(Name = "Earth's Natural Resources, Gas and Petroleum Systems & Equipment")]
        EarthNaturalResourcesGasAndPetroleumSystemsEquipment = 7,

      
    }
}
