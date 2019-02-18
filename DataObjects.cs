using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ALPR_Core
{
    public enum CarStatus
    {
        CheckedIn,
        Notified1,
        Notified2,
        Notified3,
        Expired,
        CheckedOut,
        Cancelled
    }
 
    public class ALPR_Car
    {
        public ObjectId id;
        public string LicensePlate;
        public int ParkingSpot;
        public string TimeRecorded;
        public string State;
        public double Confidence;
        public string PictureFileName;
    }

    public class Resident_Car
    {
        public ObjectId id;
        public string LicensePlate;
        public string State;
        public int UnitNumber;
        public string CarOwnerName;
        public string CarOwnerPhone;
        public int RentedSpotNumber = 0;
    }

    public class CameraShot
    {
        public string TimeStamp;
        public string CameraName;
        public string SpotType;
        public string CarType;
        public string LicensePlate;
        public string LicensePlateState;
        public double Confidence;
        public string ViolationType;
        public string HoursParked;
    }

    public class ParkingLot
    {
        public ObjectId id;
        public List<CameraShot> CamShot_List = new List<CameraShot>();
    }

    public class Visitor_Car
    {
        public ObjectId id;
        public string LicensePlate;
        public string Time;
        public int Status;
        public string ContactPhone;
        public string HostUnitNumber;
    }

    public enum CarOwnerStatus
    {
       RESIDENT = 1,
       GUEST = 2,
       CONTRACTOR = 3
    }
    public enum ViolationType
    {
        NONE = 0,
        VISITOR_OVERTIME = 1,
        VISITOR_HOA_SPOT = 2,
        CONTRACTOR_AFTER_HRS = 3,
        CONTRACTOR_HOA_SPOT = 4,
        CONTRACTOR_VISITOR_SPOT = 5,
        UNKNOWN_CAR = 6,
        RESIDENT_VISITOR_SPOT = 7,
        RESIDENT_HOA_SPOT_NoCONTRACT = 8
    }

    public struct Camera
    {
        public string Name { get; set; }
        public string SpotType { get; set; }
        public bool Enabled { get; set; }
    }

}
