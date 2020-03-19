using Newtonsoft.Json;

using ONVIF_MediaProfilDashboard.Device;
//using ONVIF_MediaProfilDashboard.Media;
using ONVIF_MediaProfilDashboard.OnvifPTZService;
using ONVIF_MediaProfilDashboard.Media10;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ONVIF_MediaProfilDashboard
{
    using System.Timers;

    public class Control
    {
        
        //string onvifexPath = Properties.Resources.ONVIFEXPath;
        //int previousSavedConnIndex = -1;
        List<CameraConnexion> cameras = new List<CameraConnexion>();
        CameraConnexion selectedCam = null;
        String path_to_connexion_file = AppDomain.CurrentDomain.BaseDirectory + @"\login_info.json";
        //VideoParam vp = new VideoParam();
        //Media2Client mediaClient;

        MediaClient mediaClient10;
        Profile profile10;


        GetCompatibleConfigurationsRequest getcapa;

        DeviceClient deviceclient;

        private enum Direction { None, Up, Down, Left, Right };
        PTZClient ptzClient;
        OnvifPTZService.PTZSpeed velocity;
        PTZVector vector;
        PTZConfigurationOptions options;

        bool relative = false;
        bool initialised = false;
        
        Timer timer;
        Direction direction;
        float panDistance;
        float tiltDistance;
        

        UriBuilder deviceUri;
        //MediaProfile[] profiles;
        //MediaProfile[] profiles2;
        //ConfigDashboard cd;
        String[] prms = { };

        string cam_ip;
        string cam_id;
        string cam_pw;

        string camProfile;
        string ptzProfile;

        float mPtzSpeed;


        public string ErrorMessage { get; private set; }
        public int PanIncrements { get; set; } = 20;
        public int TiltIncrements { get; set; } = 20;
        public double TimerInterval { get; set; } = 1500;
                      
        public bool Initialise(string cameraAddress, string userName, string password)
        {
            bool result = false;
            cam_ip = cameraAddress;
            cam_id = userName;
            cam_pw = password;

            //ConnectCam();
            try
            {
                var messageElement = new TextMessageEncodingBindingElement()
                {
                    MessageVersion = MessageVersion.CreateVersion(
                      EnvelopeVersion.Soap12, AddressingVersion.None)
                };
                HttpTransportBindingElement httpBinding = new HttpTransportBindingElement();
                httpBinding.AuthenticationScheme = System.Net.AuthenticationSchemes.Digest;
                CustomBinding bind = new CustomBinding(messageElement, httpBinding);
                //mediaClient = new Media2Client(bind,
                //  new EndpointAddress($"http://{cameraAddress}/onvif/device_service"));
                //mediaClient.ClientCredentials.HttpDigest.AllowedImpersonationLevel =
                //  System.Security.Principal.TokenImpersonationLevel.Impersonation;
                //mediaClient.ClientCredentials.HttpDigest.ClientCredential.UserName = userName;
                //mediaClient.ClientCredentials.HttpDigest.ClientCredential.Password = password;
                ptzClient = new PTZClient(bind,
                  new EndpointAddress($"http://{cameraAddress}/onvif/device_service"));
                ptzClient.ClientCredentials.HttpDigest.AllowedImpersonationLevel =
                  System.Security.Principal.TokenImpersonationLevel.Impersonation;
                ptzClient.ClientCredentials.HttpDigest.ClientCredential.UserName = userName;
                ptzClient.ClientCredentials.HttpDigest.ClientCredential.Password = password;

                deviceclient = new DeviceClient(bind,
                  new EndpointAddress($"http://{cameraAddress}/onvif/device_service"));
                deviceclient.ClientCredentials.HttpDigest.AllowedImpersonationLevel =
                  System.Security.Principal.TokenImpersonationLevel.Impersonation;
                deviceclient.ClientCredentials.HttpDigest.ClientCredential.UserName = userName;
                deviceclient.ClientCredentials.HttpDigest.ClientCredential.Password = password;

                mediaClient10 = new MediaClient(bind,
                  new EndpointAddress($"http://{cameraAddress}/onvif/device_service"));
                mediaClient10.ClientCredentials.HttpDigest.AllowedImpersonationLevel =
                  System.Security.Principal.TokenImpersonationLevel.Impersonation;
                mediaClient10.ClientCredentials.HttpDigest.ClientCredential.UserName = userName;
                mediaClient10.ClientCredentials.HttpDigest.ClientCredential.Password = password;





                //onvif ver 1.0
                var profs = mediaClient10.GetProfiles();

                profile10 = mediaClient10.GetProfile(profs[0].token);

                

                //string string2222 = deviceclient.GetWsdlUrl();




                var configs = ptzClient.GetConfigurations();

                options = ptzClient.GetConfigurationOptions(configs[0].token);

                velocity = new OnvifPTZService.PTZSpeed()
                {
                    PanTilt = new OnvifPTZService.Vector2D()
                    {
                        x = 0,
                        y = 0,
                        space = options.Spaces.ContinuousPanTiltVelocitySpace[0].URI,
                    },
                    Zoom = new OnvifPTZService.Vector1D()
                    {
                        x = 0,
                        space = options.Spaces.ContinuousZoomVelocitySpace[0].URI,
                    }
                };             

                ErrorMessage = "";
                result = initialised = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return result;
        }

      


        public void Move(string input, float speed)
        {
            mPtzSpeed = speed;
            switch(input)
            {
                case "L":
                    {
                        //"-1"값 디바이스마다 확인 필요 03.11.yj
                        velocity.PanTilt.x = mPtzSpeed * -1;
                        velocity.PanTilt.y = 0;
                        break;
                    }
                case "R":
                    {
                        velocity.PanTilt.x = mPtzSpeed * 1;
                        velocity.PanTilt.y = 0;
                        break;
                    }
                case "U":
                    {
                        velocity.PanTilt.x = 0;
                        velocity.PanTilt.y = mPtzSpeed * 1;
                        break;
                    }
                case "D":
                    {
                        velocity.PanTilt.x = 0;
                        velocity.PanTilt.y = mPtzSpeed * -1;
                        break;
                    }
                case "UL":
                    {
                        velocity.PanTilt.x = mPtzSpeed * -1;
                        velocity.PanTilt.y = mPtzSpeed * 1;
                        break;
                    }
                case "UR":
                    {
                        velocity.PanTilt.x = mPtzSpeed * 1;
                        velocity.PanTilt.y = mPtzSpeed * 1;
                        break;
                    }
                case "DL":
                    {
                        velocity.PanTilt.x = mPtzSpeed * -1;
                        velocity.PanTilt.y = mPtzSpeed * -1;
                        break;
                    }
                case "DR":
                    {
                        velocity.PanTilt.x = mPtzSpeed * 1;
                        velocity.PanTilt.y = mPtzSpeed * -1;
                        break;
                    }
                case "ZI":
                    {
                        velocity.Zoom.x = mPtzSpeed * 1;
                        break;
                    }
                case "ZO":
                    {
                        velocity.Zoom.x = mPtzSpeed * -1;
                        break;
                    }
                default :                    
                    break;

            }
            ptzClient.ContinuousMove(profile10.token, velocity, "PT10S");
            ptzClient.Stop(profile10.token, true, true);
        }

        

        public void Stop()
        {
            if (initialised)
            {
                if (relative)
                    timer.Enabled = false;
                //direction = Direction.None;
                ptzClient.Stop(profile10.token, true, true);
            }
        }        
                     
    }
}
