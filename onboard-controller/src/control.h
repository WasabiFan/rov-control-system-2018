#include "common.h"

struct ControlState
{
    bool isEnabled = false;
    Vector6f rigidForces;
};

class Control
{
  private:
    ControlState controlState;

    // TODO: don't use random intrinsics
    Matrix<float, 6, 6> intrinsics = Matrix<float, 6, 6>::Random();

  public:
    void disable();
};