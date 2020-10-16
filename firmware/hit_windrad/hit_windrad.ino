// pinout mapping
const int ROTOR_PWM         = 13; // usually also holds the onboard LED
const int ROTOR_DIRECTION_0 = A0;
const int ROTOR_DIRECTION_1 = A1;
const int LIGHT1_PWM        = 9;
const int LIGHT2_PWM        = 10;

// calibration data
const int ROTOR_MIN_SPEED   = 120;
const int ROTOR_MAX_SPEED   = 255;
const int ROTOR_WEIRD_NOISE = 50;

// temp data storage
String serialInputBuffer = "";
int targetRotorSpeed = 0;
int actualRotorSpeed = 0;
int actualRoom1Light = 255;
int actualRoom2Light = 255;


void setup() {
  // init serial communication
  Serial.begin(9600);
  
  // init pinouts
  pinMode(ROTOR_PWM, OUTPUT);
  pinMode(ROTOR_DIRECTION_0, OUTPUT);
  pinMode(ROTOR_DIRECTION_1, OUTPUT);
  pinMode(LIGHT1_PWM, OUTPUT);
  pinMode(LIGHT2_PWM, OUTPUT);

  digitalWrite(ROTOR_PWM, LOW);           // switch rotor off
  digitalWrite(ROTOR_DIRECTION_0, HIGH);  // rotor moves counter-clockwise
  digitalWrite(ROTOR_DIRECTION_1, LOW);
  digitalWrite(LIGHT1_PWM, HIGH);         // switch lights on
  digitalWrite(LIGHT2_PWM, HIGH);
}

void loop() {
  // read serial input
  while (Serial.available()) {
    char rcvdChar = (char)Serial.read();
    if (('\r' == rcvdChar) || ('\n' == rcvdChar)) {
      // command is finished -> execute
      if (0 < serialInputBuffer.length()) {
        executeCommand(serialInputBuffer);
      }
      serialInputBuffer = "";
    } else {
      // command is still being received
      serialInputBuffer += rcvdChar;
    }
  }

  // change speeds/brightnesses slowly (do not abruptly start/stop things)
  if (targetRotorSpeed > actualRotorSpeed) {
    actualRotorSpeed++;
    if (actualRotorSpeed < ROTOR_MIN_SPEED) {
      actualRotorSpeed = ROTOR_MIN_SPEED;
    }
  }
  if (targetRotorSpeed < actualRotorSpeed) {
    actualRotorSpeed--;
    if (actualRotorSpeed < ROTOR_WEIRD_NOISE) {
      actualRotorSpeed = 0;
    }
  }

  analogWrite(ROTOR_PWM, actualRotorSpeed);
  analogWrite(LIGHT1_PWM, actualRoom1Light);
  analogWrite(LIGHT2_PWM, actualRoom2Light);
  
  delay(100);
}

void executeCommand(String command) {
  /*
   * command format: X00 with
   *  X  : either 'A' for rotor, 'B' for light in room1, 'C' for light in room2
   *  000: percentage of maximum speed/brightness [0,99] (zero-padded on the left)
   */
  char actor;
  int speed;

  if (3 == command.length()) {
    
    // detect actor
    actor = command.charAt(0);
    
    // parse speed (Oh I fucking hate how C handles strings!)
    char temp[2];
    temp[0] = command.charAt(1);
    temp[1] = command.charAt(2);
    speed = atoi(temp);

    switch (actor) {
      case 'A': // rotor
        targetRotorSpeed = MapRotorSpeed(speed);
        Serial.println("Target rotor speed is now " + String(targetRotorSpeed));
        break;
      case 'B': // room1
        actualRoom1Light = MapBrightness(speed);
        Serial.println("Target room 1 brightness is now " + String(actualRoom1Light));
        break;
      case 'C': // room2
        actualRoom2Light = MapBrightness(speed);
        Serial.println("Target room 2 brightness is now " + String(actualRoom2Light));
        break;
      default: // hu?
        Serial.println("Valid command for invalid actor received and ignored.");
        break;
    }
    
  } else {
    // A bad command was sent, ignore it
    Serial.println("Bad command received and ignored: " + command + ". Length: " + String(command.length()));
  }
}

int MapBrightness(int percentage) {
  // dirty hack
  if (0 == percentage) return 0;
  if (99 == percentage) return 255;

  // map percentage [0,99] linearly to [0,255]
  return round(255 * (percentage / 100.0));
}

int MapRotorSpeed(int percentage) {
  // dirty hack
  if (0 == percentage) return 0;
  if (99 == percentage) return ROTOR_MAX_SPEED;

  // map percentage [0,99] linearly to [ROTOR_MIN_SPEED, ROTOR_MAX_SPEED]
  return round(ROTOR_MIN_SPEED + ((ROTOR_MAX_SPEED - ROTOR_MIN_SPEED) * (percentage / 100.0)));
}
