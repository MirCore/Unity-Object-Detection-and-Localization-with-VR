library(ggplot2)
log <- read.csv2("D:/UnityProjects/Unity-Object-Detection-and-Localization-with-VR/Assets/Output/log.csv")
plot <- ggplot(log, aes(Time)) + ylim(2, -6) +
  geom_ribbon(aes(ymin = GroundTruth.x - KalmanP.x, ymax = GroundTruth.x + KalmanP.x), alpha=0.5, fill="#AAAA00") +
  geom_line(aes(y = GroundTruth.x), linetype="dotted", linewidth=1) +
  geom_line(aes(y = GroundTruth.y), linetype="dotted", linewidth=1) +
  geom_point(aes(y = Measurement.x), alpha=0.5, shape=3) +
  geom_line(aes(y = Kalman.x), color="blue")