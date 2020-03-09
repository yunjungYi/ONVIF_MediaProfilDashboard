
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
            string address = "192.168.0.15:80";
            string user = "admin";
            string password = "rlrksurv@!";
            string cam_profile = "MediaProfile000";
            string ptz_profile = "000";


            Console.WriteLine("build end");
                       
            Control c = new Control();
            int loopstop = 0 ;
            c.Initialise(address, user, password);

            //c.SetCam(address, user, password, cam_profile, ptz_profile);
            c.PanLeft();
            c.Stop();

            while (true)
            {
                Console.WriteLine("getprofileend / 1:up ..");
                int input = Convert.ToInt32(Console.ReadLine());
                if (loopstop == 1)
                    break;
                switch (input)
                {
                    case 1:
                        c.TiltUp();
                        break;
                    case 2:
                        c.TiltDown();
                        break;
                    case 3:
                        {
                            c.PanLeft();
                            c.Stop();
                            break;
                        }
                    case 4:
                        c.PanRight();
                        break;
                    defalut:
                        loopstop = 1;
                        break;
                }
            }


           
            
            


            //c.ConnectCam();

        }

    }
}
