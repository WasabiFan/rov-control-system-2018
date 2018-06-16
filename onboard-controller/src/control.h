#pragma once

#include "common.h"

#define NUM_THRUSTERS 6

#if NUM_THRUSTERS != 6
#error "Square matrix is required. Please invent another dimension."
#endif

#define PWM_PRECISION_BITS 12
#define PWM_RANGE_MAX ((1 << PWM_PRECISION_BITS) - 1)

#define PPM_MIN_TIME_S (1e-3)
#define PPM_MAX_TIME_S (2e-3)

#define PPM_BASE_FREQUENCY (1/(PPM_MAX_TIME_S*1.1))
#define PPM_MIN_DUTY_CYCLE (PPM_MIN_TIME_S*PPM_BASE_FREQUENCY*PWM_RANGE_MAX)
#define PPM_MAX_DUTY_CYCLE (PPM_MAX_TIME_S*PPM_BASE_FREQUENCY*PWM_RANGE_MAX)

#define STATUS_LED_PIN (13)
#define ENABLED_BLINK_INTERVAL (250)

struct ControlState
{
  bool isEnabled = false;
};

struct TelemetryInfo
{
  bool isScalingAtLimit = false;
  float limitScaleFactor;
  Eigen::Vector6f lastOutputs;
};

struct Thruster
{
  uint8_t pwmPin;
  Eigen::Vector3f position;
  Eigen::Vector3f orientation;
};

struct DesignInfo
{
  std::array<Thruster, NUM_THRUSTERS> thrusters;
  Eigen::Vector3f centerOfMass;

  uint8_t gripperUpDownPin;
  uint8_t gripperOpenClosePin;
  
  uint8_t gimbalPin;

  double minGimbalPosition;
  double maxGimbalPosition;
  
  uint8_t extendSlidePin;
};

class Control
{
private:
  ControlState controlState;
  TelemetryInfo telemetryInfo;
  DesignInfo design;

  Eigen::Matrix<float, 6, 6> intrinsics;

  elapsedMillis blinkTimerElapsed;
  bool blinkState = false;

  void updateThrusterOutputs(Eigen::Vector6f thrusterOutputs);
  void stopAllOutputs();

  void writeMotorController29(uint8_t pin, float output);

public:
  void init(DesignInfo& design);
  void update();
  void setRequestedRigidForcesPct(Eigen::Vector6f newForcesPct);
  void setGripperOutputs(float upDown, float openClose, float extendRetract);
  void setGimbalOutputs(float upDown);
  void enable();
  void disable();
  void setIsEnabled(bool isEnabled);

  bool isEnabled();
  TelemetryInfo getTelemetryInfo();
  std::string getIntrinsicsDebugInfo();
};