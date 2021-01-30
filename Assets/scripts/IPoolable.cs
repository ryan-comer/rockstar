using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.scripts
{
    public interface IPoolable
    {
        void LeavePool();
        void ReturnToPool();
    }
}
