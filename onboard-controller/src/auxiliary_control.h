#include <Adafruit_Sensor.h>
#include <Adafruit_BNO055.h>
#include <utility/imumaths.h>

class AuxiliaryControls
{
private:
  Adafruit_BNO055 imu;
  bool isImuInitialized = false;

public:
  AuxiliaryControls() : imu(55) {}
  void init();
  imu::vector<3> getOrientation();
};