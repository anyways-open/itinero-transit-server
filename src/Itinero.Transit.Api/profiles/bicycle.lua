name = "bicycle"
vehicle_types = { "vehicle", "bicycle" }

speed = 15

speed_profile = {
    ["primary"] = { speed = speed, access = true },
    ["primary_link"] = { speed = speed, access = true },
    ["secondary"] = { speed = speed, access = true },
    ["secondary_link"] = { speed = speed, access = true },
    ["tertiary"] = { speed = speed, access = true },
    ["tertiary_link"] = { speed = speed, access = true },
    ["unclassified"] = { speed = speed, access = true },
    ["residential"] = { speed = speed, access = true },
    ["service"] = { speed = speed, access = true },
    ["services"] = { speed = speed, access = true },
    ["road"] = { speed = speed, access = true },
    ["track"] = { speed = speed, access = true },
    ["cycleway"] = { speed = speed, access = true },
    ["footway"] = { speed = speed, access = false },
    ["pedestrian"] = { speed = speed, access = false },
    ["path"] = { speed = speed, access = true },
    ["living_street"] = { speed = speed, access = true },
    ["ferry"] = { speed = speed, access = true },
    ["movable"] = { speed = speed, access = true },
    ["shuttle_train"] = { speed = speed, access = true },
    ["default"] = { speed = speed, access = true }
}

access_values = {
    ["private"] = false,
    ["yes"] = true,
    ["no"] = false,
    ["permissive"] = true,
    ["destination"] = true,
    ["customers"] = false,
    ["designated"] = true,
    ["public"] = true,
    ["delivery"] = true,
    ["use_sidepath"] = false
}

function can_access(attributes, result)
    local last_access
    local access = access_values[attributes.access]
    if access then
        last_access = access
    end
    for i = 0, 10 do
        local access_key_key = vehicle_types[i]
        local access_key = attributes[access_key_key]
        if access_key then
            access = access_values[access_key]
            if access then
                last_access = access
            end
        end
    end
    return last_access
end

-- turns a oneway tag value into a direction
function is_oneway(attributes, name)
    local oneway = attributes[name]
    if oneway == "yes" or oneway == "true" or oneway == "1" then
        return 1
    end
    if oneway == "-1" then
        return 2
    end
    if oneway == "no" then
        return 0
    end
    return nil
end

function factor(attributes, result)
    result.forward = 0
    result.backward = 0

    if not attributes then
        return
    end

    local highway = attributes.highway

    -- set highway to ferry when ferry.
    local route = attributes.route;
    if route == "ferry" then
        highway = "ferry"
    end

    -- get speed and access per highway type.
    local highway_speed = speed_profile[highway]
    if highway_speed then
        -- set speeds.
        result.forward_speed = (highway_speed.speed / 3.6)
        result.backward_speed = result.forward_speed

        -- set factors.
        result.forward = 1 / (highway_speed.speed / 3.6)
        result.backward = result.forward
        result.access = highway_speed.access
    else
        return
    end

    -- favour dedicated cycling infrastructure.
    local class_factor = classification_factors[attributes.highway]
    if class_factor ~= nil then
        result.forward = result.forward / class_factor
        result.backward = result.backward / class_factor
    end

    -- determine access.
    local access = can_access(attributes, result)
    if not access == nil then
        result.access = access
    end

    if result.access then
    else
        result.forward = 0
        result.backward = 0
        return
    end

    -- get directional information 
    -- reset forward/backward factors
    local junction = attributes.junction
    if junction == "roundabout" then
        result.direction = 1
    end
    local direction = is_oneway(attributes, "oneway")
    if direction != nil then
        result.direction = direction
    end
    local direction = is_oneway(attributes, "oneway:bicycle")
    if direction != nil then
        result.direction = direction
    end

    if result.direction == 1 then
        result.backward = 0
    elseif result.direction == 2 then
        result.forward = 0
    end
end

highest_avoid_factor = 0.5
avoid_factor = 0.7
prefer_factor = 2
highest_prefer_factor = 3

-- multiplication factors per classification
-- avoid higher classified roads
-- prefer dedicated cycling things
classification_factors = {
    ["primary"] = highest_avoid_factor,
    ["primary_link"] = highest_avoid_factor,
    ["secondary"] = avoid_factor,
    ["secondary_link"] = avoid_factor,
    ["tertiary"] = avoid_factor,
    ["tertiary_link"] = avoid_factor,
    ["residential"] = 1,
    ["path"] = prefer_factor,
    ["cycleway"] = prefer_factor,
    ["footway"] = prefer_factor,
    ["pedestrian"] = avoid_factor,
    ["steps"] = avoid_factor
}
