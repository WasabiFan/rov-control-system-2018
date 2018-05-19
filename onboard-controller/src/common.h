#pragma once

#include <Eigen.h>
#include <Dense>
#include "Arduino.h"

#define DEBUG

#ifdef DEBUG
#define DEBUG_SERIAL Serial4
#define DEBUG_SERIAL_BEGIN() DEBUG_SERIAL.begin(115200)
#define DEBUG_SERIAL_PRINT(MESSAGE) DEBUG_SERIAL.print(MESSAGE)
#define DEBUG_SERIAL_PRINTLN(MESSAGE) DEBUG_SERIAL.println(MESSAGE)
#else
#define DEBUG_SERIAL
#define DEBUG_SERIAL_BEGIN()
#define DEBUG_PRINT(MESSAGE)
#define DEBUG_PRINTLN(MESSAGE)
#endif

namespace Eigen
{
    typedef Eigen::Matrix<float, 6, 1> Vector6f;
}