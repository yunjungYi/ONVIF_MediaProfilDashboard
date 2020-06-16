
using Newtonsoft.Json;

using ONVIF_MediaProfilDashboard.Device;
using ONVIF_MediaProfilDashboard.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;


using ONVIF_MediaProfilDashboard;

namespace ONVIF_MediaProfilDashboard
{
    public partial class ConnectCamera 
    {
        static void Main()
        {
            string address = "192.168.0.139:80";
            string user = "admin";
            string password = "admin123";            

            
            Console.WriteLine("build end");
                       
            Control c = new Control();
            int loopstop = 1 ;
            c.Initialise(address, user, password);

           
            //c.SetToken(cam_profile, ptz_profile);

            //c.SetCam(address, user, password, cam_profile, ptz_profile);
            float setvelocity = 1f;
            while (true)
            {
                Console.WriteLine("getprofileend /L,R,U,D,UR,UD q=end..");
                
                string input = Console.ReadLine();
                if (input == "q" || input == "Q")
                    break;
                c.Move(input, setvelocity);

            }                            
            
            //c.ConnectCam();

        }

    }
}
