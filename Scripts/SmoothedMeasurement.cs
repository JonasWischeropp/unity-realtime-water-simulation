using System;

// Not generic because older C# versions wouldn't support it. 
public class SmoothedMeasurement {
    private float[] _measurements;
    private float _sum;

    private int _currentIndex;
    private int _fillAmount;
    
    private readonly uint _sampleCount;
    
    public SmoothedMeasurement(uint sampleCount) {
        _sampleCount = sampleCount;
        if (sampleCount <= 1)
            throw new ArgumentException("The sample count should be greater than one");
        Reset();
    }
    
    public void AddMeasurement(float value) {
        if (_fillAmount < _measurements.Length)
            _fillAmount += 1;
        
        _sum += value;
        _sum -= _measurements[_currentIndex];
        _measurements[_currentIndex] = value;
        _currentIndex = (_currentIndex + 1) % _measurements.Length;
    }
    
    public float Value() {
        return _sum / _fillAmount;
    }
    
    public bool Ready() {
        return _fillAmount != 0;
    }
    
    public void Reset() {
        _measurements = new float[_sampleCount];
        _sum = 0f;
        _currentIndex = 0;
        _fillAmount = 0;
    }
}
