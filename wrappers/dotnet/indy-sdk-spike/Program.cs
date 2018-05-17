using System;
using System.Threading.Tasks;

namespace indy_sdk_spike
{
    class Program
    {
        static void Main(string[] args)
        {

            Task.WaitAll(new SpikeApp().RuntrusteeCollegeDemo());
        }
    }
}
