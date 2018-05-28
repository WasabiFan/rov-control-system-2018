#include <Audio.h>
#include <Servo.h>

//#define DUMP_MODE

AudioInputAnalog audioIn(A2);

AudioAnalyzeFFT1024 fft;
AudioConnection patchCord1(audioIn, fft);

#define NO_TIME (UINT32_MAX)
#define TONE_TIME_THRESH 1000
#define TONE_RESET_TIME_THRESH 500

#define SERVO_PIN (2)
#define SERVO_CLOSED_POS 100
#define SERVO_OPEN_POS 80
#define SERVO_RESET_TIME_THRESH (4000)

uint8_t detectionStage = 0;
const int frequencyBuckets[] = {19, 22};
uint32_t lastStageEndTime = NO_TIME;
uint32_t detectionStartTime = NO_TIME;
uint32_t triggerTime = NO_TIME;

Servo servo;

void setup()
{
    Serial.begin(115200);
    AudioMemory(12);
    servo.attach(SERVO_PIN);
    servo.write(SERVO_CLOSED_POS);
}

void beginLogMessage()
{
    Serial.print("[");
    Serial.print(millis());
    Serial.print("] ");
}

void activateTrigger()
{
    beginLogMessage();
    Serial.println("TRIGGER");
    triggerTime = millis();
    servo.write(SERVO_OPEN_POS);
}

void updateServoReset()
{
    if (triggerTime != NO_TIME && millis() - triggerTime > SERVO_RESET_TIME_THRESH)
    {
        beginLogMessage();
        Serial.println("Trigger time expired; resetting");

        servo.write(SERVO_CLOSED_POS);
        triggerTime = NO_TIME;
    }
}

bool checkIsFreqActive(int bucketNumber)
{
    return fft.read(bucketNumber) > 0.04;
}

void updateTriggerCheck()
{
    if (fft.available())
    {
        uint32_t time = millis();
        bool isFreqActive = checkIsFreqActive(frequencyBuckets[detectionStage]);
        bool isLastFreqActive = detectionStage > 0 && checkIsFreqActive(frequencyBuckets[detectionStage - 1]);

        if (isFreqActive && isLastFreqActive)
        {
            beginLogMessage();
            Serial.println("Warn: Active frequency rejected because of simultaneous past stage frequency");
        }

        if (isFreqActive && !isLastFreqActive)
        {
            if (detectionStartTime == NO_TIME)
            {
                beginLogMessage();
                Serial.println("Initial active edge detected");
                detectionStartTime = time;
            }
            else if (time - detectionStartTime > TONE_TIME_THRESH)
            {
                beginLogMessage();
                Serial.print("Accepted stage ");
                Serial.print(detectionStage);
                Serial.println("; advancing");

                detectionStage++;
                lastStageEndTime = time;
                detectionStartTime = NO_TIME;

                if (detectionStage >= sizeof(frequencyBuckets) / sizeof(int))
                {
                    activateTrigger();
                    detectionStage = 0;
                    lastStageEndTime = NO_TIME;
                }
            }
        }
        else
        {
            detectionStartTime = NO_TIME;
            if (lastStageEndTime != NO_TIME && time - lastStageEndTime > TONE_RESET_TIME_THRESH)
            {
                beginLogMessage();
                Serial.print("Timed out waiting for stage ");
                Serial.println(detectionStage);
                lastStageEndTime = NO_TIME;
                detectionStage = 0;
            }
        }
    }
}

void printSpectrum()
{
    float n;
    int i;
    if (fft.available())
    {
        Serial.print("FFT: ");
        for (i = 0; i < 40; i++)
        {
            n = fft.read(i);
            if (n >= 0.01)
            {
                Serial.print(n);
                Serial.print(" ");
            }
            else
            {
                Serial.print("     ");
            }
        }
        Serial.println();

        Serial.print("     ");
        for (i = 0; i < 40; i++)
        {
            Serial.print(i);
            if (i < 10)
            {
                Serial.print("    ");
            }
            else
            {
                Serial.print("   ");
            }
        }
        Serial.println();
    }
}

void loop()
{
#ifdef DUMP_MODE
    printSpectrum();
#else
    updateTriggerCheck();
    updateServoReset();
#endif
}
