/*
	\author Leonardo Molina (leonardomt@gmail.com).
	\version 1.1.180615
*/

const uint32_t triggerDuration = 30000;		// How long to trigger the valve for (us).
const uint8_t encoderHids[] = {2, 4};		// Rotary encoder pins.
const uint8_t triggerHid = 8;				// Trigger pin.

volatile int8_t step = 0;
int8_t lastStep = 0;
uint32_t triggerEnd = 0;

void setup() {
	Serial.begin(115200);
	
	// Setup rotary encoder pins as inputs; listen to rise updates only.
	pinMode(encoderHids[0], INPUT_PULLUP);
	pinMode(encoderHids[1], INPUT_PULLUP);
	attachInterrupt(digitalPinToInterrupt(encoderHids[0]), encoderCallback, RISING);
	
	// Setup trigger pin as outputs.
	pinMode(triggerHid, OUTPUT);
}

void loop() {
	while (Serial.available()) {
		Serial.read();
		digitalWrite(triggerHid, HIGH);
		triggerEnd = micros() + triggerDuration;
	}
	
	// Switch pin off as scheduled.
	if (triggerEnd > 0 && micros() >= triggerEnd) {
		triggerEnd = 0;
		digitalWrite(triggerHid, LOW);
	}
	
	noInterrupts();
	int8_t stepCopy = step;
	interrupts();
	
	int8_t delta = lastStep - stepCopy;
	if (delta != 0) {
		Serial.write(delta);
		lastStep = stepCopy;
	}
}

// Interrupt call due to the encoder pin change.
void encoderCallback() {
	// Direction of turning is given by comparing the encoders pin.
	step += digitalRead(encoderHids[1]) ? +1 : -1;
}