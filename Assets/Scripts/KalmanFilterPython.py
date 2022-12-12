import numpy as np
from filterpy.kalman import KalmanFilter
from filterpy.common import Q_discrete_white_noise
import UnityEngine

def datagen(N, Ts, Q):
    t = np.arange(N) * Ts

    Ad = np.atleast_2d([[1, 0, Ts, 0],
                        [0, 1, 0, Ts],
                        [0, 0, 1, 0 ],
                        [0, 0, 0, 1 ]])
    C = np.atleast_2d([[1, 0, 0, 0],
                       [0, 1, 0, 0]])
    Gd = np.atleast_2d([[Ts**2/2, 0],
                        [0, Ts**2/2],
                        [Ts/2, 0],
                        [0, Ts/2]])

    R = np.atleast_2d([[1, 0],
                       [0, 1]])

    xk = np.atleast_2d([0, 0, 0, 0]).T
    xvec = []
    yvec = []
    for k in range(N):
        vk = np.random.multivariate_normal([0,0], R).reshape(2,1)
        yk = C @ xk + vk
        xvec.append(xk)
        yvec.append(yk)
        zk = np.atleast_2d([[np.random.randn()*10*np.sqrt(Q)],
                            [np.random.randn()*10*np.sqrt(Q)]])
        xk = Ad @ xk + Gd @ zk

    xvec = np.array(xvec).reshape(-1,4)
    yvec = np.array(yvec).reshape(-1,2)

    return t, xvec, yvec
    
N = 1000
Ts = 0.1
Q = 0.1
t, xvec, yvec = datagen(N = N, Ts = Ts, Q = Q)

kf = KalmanFilter(dim_x=4, dim_z=2)
kf.x = np.array([[0],[0],[0],[0]])
kf.F = np.atleast_2d([[1, 0, Ts, 0], # State transition function
                      [0, 1, 0, Ts],
                      [0, 0, 1, 0 ],
                      [0, 0, 0, 1 ]])
kf.H = np.atleast_2d([[1, 0, 0, 0], # Measurement function
                      [0, 1, 0, 0]])
kf.P = np.diag([1000,1000,1,1]) # Covariance matrix
kf.R = np.atleast_2d([[1, 0], # Measurement noise covariance
                      [0, 1]])
kf.Q = Q_discrete_white_noise(dim = 2, dt = Ts, var = Q, block_size=2, order_by_dim=False)

xs, cov = [], []

for n in range(0, N):
    z = yvec[n]
    kf.predict()
    kf.update(z)
    
UnityEngine.Debug.Log(kf.x[0])