#include "common.h"

#define NUM_THRUSTERS 6

#if NUM_THRUSTERS != 6
#error "Square matrix is required. Please invent another dimension."
#endif

struct ControlState
{
    bool isEnabled = false;
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

    // TODO: don't use random intrinsics
    Eigen::Matrix<float, 6, 6> intrinsics = Eigen::Matrix<float, 6, 6>::Random();
    const ThrusterIO thrusterIO[NUM_THRUSTERS] = {};

    void updateThrusterOutputs(Eigen::Vector6f thrusterOutputs);
    void stopAllOutputs();

  public:
    void init();
    void updateRequestedRigidForces(Eigen::Vector6f newForces);
    void disable();
};