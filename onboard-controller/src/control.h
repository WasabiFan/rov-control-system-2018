#include "common.h"

#define NUM_THRUSTERS 6

#if NUM_THRUSTERS != 6
#error "Square matrix is required. Please invent another dimension."
#endif

struct ControlState
{
  bool isEnabled = false;
};

struct TelemetryInfo
{
  bool isScalingAtLimit = false;
  float limitScaleFactor;
};

struct ThrusterIO
{
  uint8_t pwmPin;
  uint8_t dirPin;
};

class Control
{
private:
  ControlState controlState;
  TelemetryInfo telemetryInfo;

  // TODO: don't use random intrinsics
  Eigen::Matrix<float, 6, 6> intrinsics = Eigen::Matrix<float, 6, 6>::Random();
  const ThrusterIO thrusterIO[NUM_THRUSTERS] = {};

  void updateThrusterOutputs(Eigen::Vector6f thrusterOutputs);
  void stopAllOutputs();

public:
  void init();
  void updateRequestedRigidForcesPct(Eigen::Vector6f newForcesPct);
  void disable();

  bool isEnabled();
  TelemetryInfo getTelemetryInfo();
};