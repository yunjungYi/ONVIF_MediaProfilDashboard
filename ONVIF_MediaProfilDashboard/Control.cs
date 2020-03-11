using Newtonsoft.Json;

using ONVIF_MediaProfilDashboard.Device;
using ONVIF_MediaProfilDashboard.Media;
using ONVIF_MediaProfilDashboard.OnvifPTZService;

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
        Media2Client mediaClient;

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
        MediaProfile[] profiles;
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
        
        public void ConnectCam()
        {
            bool inError = false;
            deviceUri = new UriBuilder("http:/onvif/device_service");
            string[] addr = cam_ip.Split(':');
            deviceUri.Host = addr[0];
            if (addr.Length == 2)
            {
                deviceUri.Port = Convert.ToInt16(addr[1]);
            }

            System.ServiceModel.Channels.Binding binding;
            HttpTransportBindingElement httpTransport = new HttpTransportBindingElement();
            httpTransport.AuthenticationScheme = System.Net.AuthenticationSchemes.Digest;
            binding = new CustomBinding(new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8), httpTransport);

            try
            {
                DeviceClient device = new DeviceClient(binding, new EndpointAddress(deviceUri.ToString()));
                Service[] services = device.GetServices(false);
                Service xmedia2 = services.FirstOrDefault(s => s.Namespace == "http://www.onvif.org/ver20/media/wsdl");

                if (xmedia2 != null)
                {
                    mediaClient = new Media2Client(binding, new EndpointAddress(deviceUri.ToString()));
                    mediaClient.ClientCredentials.HttpDigest.ClientCredential.UserName = cam_id;
                    mediaClient.ClientCredentials.HttpDigest.ClientCredential.Password = cam_pw;
                    mediaClient.ClientCredentials.HttpDigest.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
                    profiles = mediaClient.GetProfiles(null, null);

                    // Make sure that the list is empty before adding new items
                    //listBox.Items.Clear();
                    //if (profiles != null)
                    //    foreach (MediaProfile p in profiles)
                    //    {
                    //        listBox.Items.Add(p.Name);
                    //    }
                    // Enable Manage Profile btn
                    //create_profile_btn.IsEnabled = true;
                }
                // listBox.SelectionChanged += OnSelectionChanged;
                // video.MediaPlayer.VlcLibDirectoryNeeded += OnVlcControlNeedsLibDirectory;
                // video.MediaPlayer.Log += MediaPlayer_Log;
                // video.MediaPlayer.EndInit();
            }
            catch (Exception ex)
            {
                string Text = ex.Message;
                inError = true;

            }
            //changeErrorLogColor(inError);
        }

        public void SetToken(string setcamProfile, string setptzProfile)
        {
            camProfile = setcamProfile;
            ptzProfile = setptzProfile;
            initialised = true;
        }

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
                mediaClient = new Media2Client(bind,
                  new EndpointAddress($"http://{cameraAddress}/onvif/media_service"));
                mediaClient.ClientCredentials.HttpDigest.AllowedImpersonationLevel =
                  System.Security.Principal.TokenImpersonationLevel.Impersonation;
                mediaClient.ClientCredentials.HttpDigest.ClientCredential.UserName = userName;
                mediaClient.ClientCredentials.HttpDigest.ClientCredential.Password = password;
                ptzClient = new PTZClient(bind,
                  new EndpointAddress($"http://{cameraAddress}/onvif/ptz_service"));
                ptzClient.ClientCredentials.HttpDigest.AllowedImpersonationLevel =
                  System.Security.Principal.TokenImpersonationLevel.Impersonation;
                ptzClient.ClientCredentials.HttpDigest.ClientCredential.UserName = userName;
                ptzClient.ClientCredentials.HttpDigest.ClientCredential.Password = password;

                // onvif ver 1.0
                //var profs = mediaClient.GetProfiles(); ;
                //profile = mediaClient.GetProfile(profs[0].token);

                //onvif ver 2.0
                profiles = mediaClient.GetProfiles(null, null);


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
                if (relative)
                {
                    timer = new Timer(TimerInterval);
                    timer.Elapsed += Timer_Elapsed;
                    velocity.PanTilt.space = options.Spaces.RelativePanTiltTranslationSpace[0].URI;
                    panDistance = (options.Spaces.RelativePanTiltTranslationSpace[0].XRange.Max -
                      options.Spaces.RelativePanTiltTranslationSpace[0].XRange.Min) / PanIncrements;
                    tiltDistance = (options.Spaces.RelativePanTiltTranslationSpace[0].YRange.Max -
                      options.Spaces.RelativePanTiltTranslationSpace[0].YRange.Min) / TiltIncrements;
                }

                vector = new PTZVector()
                {
                    PanTilt = new OnvifPTZService.Vector2D()
                    {
                        x = 0,
                        y = 0,
                        space = options.Spaces.RelativePanTiltTranslationSpace[0].URI
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

        

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Move();
        }

        public void TiltUp()
        {
            if (initialised)
            {
                if (relative)
                {
                    direction = Direction.Up;
                    Move();
                }
                else
                {
                    velocity.PanTilt.x = 0;
                    velocity.PanTilt.y = options.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Max;                    
                    ptzClient.ContinuousMove(profiles[0].token, velocity, "PT10S");
                }
            }
        }

        public void TiltDown()
        {
            if (initialised)
            {
                if (relative)
                {
                    direction = Direction.Down;
                    Move();
                }
                else
                {
                    velocity.PanTilt.x = 0;
                    velocity.PanTilt.y = options.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Min;
                    velocity.PanTilt.y = -1;
                    ptzClient.ContinuousMove(profiles[0].token, velocity, "PT10S");
                }
            }
        }


        public void PanLeft()
        {
            if (initialised)
            {
                if (relative)
                {
                    direction = Direction.Left;
                    Move();
                }
                else
                {
                    velocity.PanTilt.x = options.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Min;
                    //velocity.PanTilt.x = -0.5f;
                    velocity.PanTilt.y = 0;
                    ptzClient.ContinuousMove(profiles[0].token, velocity, "PT10S");
                }
            }
        }

        public void PanRight()
        {
            if (initialised)
            {
                if (relative)
                {
                    direction = Direction.Right;
                    Move();
                }
                else
                {
                    velocity.PanTilt.x = options.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Max;
                    velocity.PanTilt.y = 0;
                    ptzClient.ContinuousMove(profiles[0].token, velocity, "PT10S");

                }
            }
        }

        public void directionmove(string input, float speed)
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
                default :
                    break;

            }
            ptzClient.ContinuousMove(profiles[0].token, velocity, "PT10S");
            ptzClient.Stop(profiles[0].token, true, true);
        }

        public void Stop()
        {
            if (initialised)
            {
                if (relative)
                    timer.Enabled = false;
                direction = Direction.None;
                ptzClient.Stop(profiles[0].token, true, true);
            }
        }

        private void Move()
        {
            bool move = true;

            switch (direction)
            {
                case Direction.Up:
                    velocity.PanTilt.x = 0;
                    velocity.PanTilt.y = options.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Max;
                    vector.PanTilt.x = 0;
                    vector.PanTilt.y = tiltDistance;
                    break;

                case Direction.Down:
                    velocity.PanTilt.x = 0;
                    velocity.PanTilt.y = options.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Max;
                    vector.PanTilt.x = 0;
                    vector.PanTilt.y = -tiltDistance;
                    break;

                case Direction.Left:
                    velocity.PanTilt.x = options.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Max;
                    velocity.PanTilt.y = 0;
                    vector.PanTilt.x = -panDistance;
                    vector.PanTilt.y = 0;
                    break;

                case Direction.Right:
                    velocity.PanTilt.x = options.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Max;
                    velocity.PanTilt.y = 0;
                    vector.PanTilt.x = panDistance;
                    vector.PanTilt.y = 0;
                    break;

                case Direction.None:
                default:
                    move = false;
                    break;
            }
            if (move)
            {
                ptzClient.RelativeMove(ptzProfile, vector, velocity);
            }
            timer.Enabled = true;
        }




    }
}
