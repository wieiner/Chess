#pragma once

#include <iostream>
#include <string>

class ContractTestRunner
{
public:
    void Check(bool condition, const std::string& name)
    {
        if (condition)
        {
            std::cout << "PASS " << name << "\n";
            std::cout.flush();
            return;
        }

        std::cout << "FAIL " << name << "\n";
        std::cout.flush();
        ++failures_;
    }

    int Finish(const std::string& suiteName) const
    {
        if (failures_ == 0)
        {
            std::cout << suiteName << ": PASS\n";
            std::cout.flush();
            return 0;
        }

        std::cout << suiteName << ": FAIL (" << failures_ << " failures)\n";
        std::cout.flush();
        return 1;
    }

private:
    int failures_ = 0;
};
