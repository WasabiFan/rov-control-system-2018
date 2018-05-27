#pragma once

#include <Adafruit_Sensor.h>
#include <Adafruit_BNO055.h>
#include <utility/imumaths.h>

#include "comms.h"

#define IMU_STATE_CONNECTED (1)
#define IMU_STATE_HAS_CALIBRATION_DATA (2)
#define IMU_STATE_FULLY_CALIBRATED (3)

#define IMU_UPDATE_INTERVAL_MS (100)

#define IMU_CALIB_DATA_EEPROM_BEGIN_ADDR (0)

#define BUZZER_PIN (6)
#define BUZZER_TONE_LENGTH (1250)
#define NO_STAGE (-1)
const uint16_t buzzerStageTones[] = { 800, 950 };

#define WATER_SENSOR_PIN (23)

class AuxiliaryControl
{
private:
  Adafruit_BNO055 imu;
  int imuState = 0;

  elapsedMillis timeSinceImuUpdate = 0;

  int currentBuzzerStage = -1;
  elapsedMillis timeInBuzzerStage;

  void tryLoadCalib();
  void updateImu();

  void debugPrintSensorOffsets(const adafruit_bno055_offsets_t &calibData);

public:
  AuxiliaryControl() : imu(55) {}
  void init();
  void update();

  void setIsBuzzerPlaying(bool isPlaying);

  bool checkLeakSensor();
  int getCalibStatus(uint8_t &system, uint8_t &gyro, uint8_t &accel, uint8_t &mag);
  imu::Vector<3> getOrientation();
};