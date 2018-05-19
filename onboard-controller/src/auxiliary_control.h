#include <Adafruit_Sensor.h>
#include <Adafruit_BNO055.h>
#include <utility/imumaths.h>

class AuxiliaryControl
{
private:
  Adafruit_BNO055 imu;
  bool isImuInitialized = false;

public:
  AuxiliaryControl() : imu(55) {}
  void init();
  imu::Vector<3> getOrientation();
};