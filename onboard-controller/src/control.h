#pragma once

#include "common.h"

#define NUM_THRUSTERS 6

#if NUM_THRUSTERS != 6
#error "Square matrix is required. Please invent another dimension."
#endif

#define PWM_PRECISION_BITS 12
#define PWM_RANGE_MAX ((1 << PWM_PRECISION_BITS) - 1)

#define THRUSTER_MIN_TIME_S (1e-3)
#define THRUSTER_MAX_TIME_S (2e-3)

#define THRUSTER_BASE_FREQUENCY (1/(THRUSTER_MAX_TIME_S*1.1))
#define THRUSTER_MIN_DUTY_CYCLE (THRUSTER_MIN_TIME_S*THRUSTER_BASE_FREQUENCY*PWM_RANGE_MAX)
#define THRUSTER_MAX_DUTY_CYCLE (THRUSTER_MAX_TIME_S*THRUSTER_BASE_FREQUENCY*PWM_RANGE_MAX)

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
};

class Control
{
private:
  ControlState controlState;
  TelemetryInfo telemetryInfo;
  DesignInfo design;

  // TODO: don't use random intrinsics
  Eigen::Matrix<float, 6, 6> intrinsics;

  void updateThrusterOutputs(Eigen::Vector6f thrusterOutputs);
  void stopAllOutputs();

  void writeMotorController29(uint8_t pin, float output);

public:
  void init(DesignInfo& design);
  void updateRequestedRigidForcesPct(Eigen::Vector6f newForcesPct);
  void setGripperOutputs(float upDown, float openClose);
  void setGimbalOutputs(float upDown);
  void disable();

  bool isEnabled();
  TelemetryInfo getTelemetryInfo();
  std::string getIntrinsicsDebugInfo();
};