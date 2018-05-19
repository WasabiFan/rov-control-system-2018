#pragma once

#include <Adafruit_Sensor.h>
#include <Adafruit_BNO055.h>
#include <utility/imumaths.h>

#include "comms.h"

#define IMU_STATE_CONNECTED (1)
#define IMU_STATE_HAS_CALIBRATION_DATA (2)
#define IMU_STATE_FULLY_CALIBRATED (3)

class AuxiliaryControl
{
private:
  Adafruit_BNO055 imu;
  int imuState = 0;

public:
  AuxiliaryControl() : imu(55) {}
  void init();
  void updateTracking();

  imu::Vector<3> getOrientation();
};