#pragma once

#include <Eigen.h>
#include <Dense>
#include "Arduino.h"

#define DEBUG

#ifdef DEBUG
#define DEBUG_SERIAL Serial4
#define DEBUG_SERIAL_BEGIN() DEBUG_SERIAL.begin(115200)
#define DEBUG_SERIAL_LINE_START() { DEBUG_SERIAL.print("["); DEBUG_SERIAL.print(millis()); DEBUG_SERIAL.print("] "); }
#define DEBUG_SERIAL_PRINT(MESSAGE) DEBUG_SERIAL.print(MESSAGE)
#define DEBUG_SERIAL_IPRINT(MESSAGE) { DEBUG_SERIAL_LINE_START(); DEBUG_SERIAL_PRINT(MESSAGE); }
#define DEBUG_SERIAL_PRINTLN(MESSAGE) DEBUG_SERIAL.println(MESSAGE)
#define DEBUG_SERIAL_IPRINTLN(MESSAGE) { DEBUG_SERIAL_LINE_START(); DEBUG_SERIAL_PRINTLN(MESSAGE); }
#else
#define DEBUG_SERIAL
#define DEBUG_SERIAL_BEGIN()
#define DEBUG_SERIAL_LINE_START()
#define DEBUG_PRINT(MESSAGE)
#define DEBUG_IPRINT(MESSAGE)
#define DEBUG_PRINTLN(MESSAGE)
#define DEBUG_IPRINTLN(MESSAGE)
#endif

namespace Eigen
{
    typedef Eigen::Matrix<float, 6, 1> Vector6f;
}