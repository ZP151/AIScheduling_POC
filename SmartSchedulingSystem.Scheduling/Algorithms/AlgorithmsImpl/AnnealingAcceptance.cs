using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchedulingSystem.Scheduling.Algorithms.AlgorithmsImpl
{
    public static class AnnealingAcceptance
    {
        public static bool ShouldAccept(double delta, double temperature)
        {
            if (delta > 0)
                return true; // 得分更高，直接接受

            double probability = Math.Exp(delta / temperature);
            return new Random().NextDouble() < probability;
        }
    }

}
