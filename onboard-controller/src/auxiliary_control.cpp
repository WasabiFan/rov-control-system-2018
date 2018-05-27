#include "auxiliary_control.h"

#include "common.h"

#include <EEPROM.h>

void AuxiliaryControl::init()
{
    bool isImuInitialized = this->imu.begin();
    if (isImuInitialized)
    {
        this->imuState = IMU_STATE_CONNECTED;

        tryLoadCalib();
    }
    else
    {
        Comms::getInstance().logError("Failed to initialize IMU. Orientation will be unavailable.");
    }
    // setExtCrystalUse

}

void AuxiliaryControl::tryLoadCalib()
{
    int memAddress = IMU_CALIB_DATA_EEPROM_BEGIN_ADDR;
    long bnoID;
    EEPROM.get(memAddress, bnoID);

    sensor_t sensor;
    imu.getSensor(&sensor);
    if (bnoID != sensor.sensor_id)
    {
        Comms::getInstance().logError("No Calibration Data for this sensor exists in EEPROM");
    }
    else
    {
        DEBUG_SERIAL_IPRINTLN("Found calibration for this sensor in EEPROM.");

        memAddress += sizeof(long);
        adafruit_bno055_offsets_t calibrationData;
        EEPROM.get(memAddress, calibrationData);

        DEBUG_SERIAL_IPRINTLN("Restoring calibration data to the BNO055...");
        imu.setSensorOffsets(calibrationData);

        debugPrintSensorOffsets(calibrationData);

        DEBUG_SERIAL_IPRINTLN("Calibration data loaded into BNO055");
        imuState = IMU_STATE_HAS_CALIBRATION_DATA;
    }
}

void AuxiliaryControl::update()
{
    uint32_t time = millis();
    if (time - lastImuUpdate > IMU_UPDATE_INTERVAL_MS) {
        lastImuUpdate = time;
        updateImu();
    }
}

void AuxiliaryControl::updateImu()
{
    if (imuState >= IMU_STATE_CONNECTED && imuState < IMU_STATE_FULLY_CALIBRATED)
    {
        if (imu.isFullyCalibrated())
        {
            if (imuState < IMU_STATE_HAS_CALIBRATION_DATA)
            {
                adafruit_bno055_offsets_t newCalib;
                imu.getSensorOffsets(newCalib);

                DEBUG_SERIAL_IPRINTLN("Storing calibration data to EEPROM...");

                debugPrintSensorOffsets(newCalib);

                sensor_t sensor;
                imu.getSensor(&sensor);

                int memAddress = IMU_CALIB_DATA_EEPROM_BEGIN_ADDR;
                EEPROM.put(memAddress, sensor.sensor_id);

                memAddress += sizeof(long);
                EEPROM.put(memAddress, newCalib);
                DEBUG_SERIAL_IPRINTLN("Data stored to EEPROM.");

                imuState = IMU_STATE_HAS_CALIBRATION_DATA;
            }

            imuState = IMU_STATE_FULLY_CALIBRATED;
        }
        else
        {
            sensors_event_t event;
            imu.getEvent(&event);
        }
    }
}

void AuxiliaryControl::debugPrintSensorOffsets(const adafruit_bno055_offsets_t &calibData)
{
    DEBUG_SERIAL_IPRINT("Accelerometer: ");
    DEBUG_SERIAL_PRINT(calibData.accel_offset_x); DEBUG_SERIAL_PRINT(" ");
    DEBUG_SERIAL_PRINT(calibData.accel_offset_y); DEBUG_SERIAL_PRINT(" ");
    DEBUG_SERIAL_PRINTLN(calibData.accel_offset_z);

    DEBUG_SERIAL_IPRINT("Gyro: ");
    DEBUG_SERIAL_PRINT(calibData.gyro_offset_x); DEBUG_SERIAL_PRINT(" ");
    DEBUG_SERIAL_PRINT(calibData.gyro_offset_y); DEBUG_SERIAL_PRINT(" ");
    DEBUG_SERIAL_PRINTLN(calibData.gyro_offset_z);

    DEBUG_SERIAL_IPRINT("Mag: ");
    DEBUG_SERIAL_PRINT(calibData.mag_offset_x); DEBUG_SERIAL_PRINT(" ");
    DEBUG_SERIAL_PRINT(calibData.mag_offset_y); DEBUG_SERIAL_PRINT(" ");
    DEBUG_SERIAL_PRINTLN(calibData.mag_offset_z);

    DEBUG_SERIAL_IPRINT("Accel Radius: ");
    DEBUG_SERIAL_PRINTLN(calibData.accel_radius);

    DEBUG_SERIAL_IPRINT("Mag Radius: ");
    DEBUG_SERIAL_PRINTLN(calibData.mag_radius);
}


int AuxiliaryControl::getCalibStatus(uint8_t &system, uint8_t &gyro, uint8_t &accel, uint8_t &mag)
{
    imu.getCalibration(&system, &gyro, &accel, &mag);
    return this->imuState;
}

imu::Vector<3> AuxiliaryControl::getOrientation()
{
    if (!(this->imuState >= IMU_STATE_CONNECTED))
    {
        return imu::Vector<3>();
    }
    return imu.getQuat().toEuler();
}
