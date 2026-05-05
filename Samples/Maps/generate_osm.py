import math

############
## CONFIG ##
############
road_len = 1000  # in meters
node_sampling = 0.5  # meters between nodes


road_centre_lat = 0.0000
road_centre_lon = 0.005  # Center longitude for circular road
m_to_lat = 0.009 / 1000  # ~approx conversion from meters to lat/lon

# Road shape: 
# 0 - straight, 
# 1 - sinusoidal, 
# 2 - circular
road_shape = 1  
amplitude = 10      # amplitude of the sinusoid in meters (not used for circular)
frequency = 0.05    # frequency of the sinusoid (not used for circular)
radius = 100         # radius of the circle in meters

road_end_lon = road_len * m_to_lat

xml_header = f"""<?xml version='1.0' encoding='UTF-8'?>
<osm version="0.6" generator="IRT">
  <bounds minlat="-0.0005" minlon="0.0000" maxlat="0.0005" maxlon="{road_end_lon:.6f}"/>
"""

nodes = ""
ways = ""

node_id = 1
way_id = 100

######################
# Main road geometry #
######################


road_node_ids = []

if road_shape == 2:
    # Calculate total number of nodes needed for a complete circle based on circumference
    circumference = 2 * math.pi * radius
    num_nodes_circle = int(circumference / node_sampling)

    # Insert nodes along the circular path at specified intervals based on node_sampling
    for i in range(num_nodes_circle):
        angle_rad = (i / num_nodes_circle) * (2 * math.pi)   # Normalize angle to [0,2π]
        
        lon_offset = radius * math.cos(angle_rad) * m_to_lat
        lat_offset = radius * math.sin(angle_rad) * m_to_lat
        
        lat = road_centre_lat + lat_offset
        lon_base_position= road_centre_lon + lon_offset

        nodes += f"""<node id="{node_id}" lat="{lat:.8f}" lon="{lon_base_position:.8f}" visible="true" version="1"/>\n"""
        road_node_ids.append(node_id)
        node_id += 1

else:
    # For straight or sinusoidal roads, insert nodes along the length defined by road_len.
    i = 0.0
    
    while i <= road_len:
        lon_offset, lat_offset = None, None
        
        if road_shape == 1:
            # Sinusoidal shape: calculate latitude using a sine function for a sinusoidal effect
            lat_variation = amplitude * math.sin(frequency * i)
            lat_offset = lat_variation * m_to_lat
            
        if road_shape == 1:
            lat = road_centre_lat + (lat_offset if lat_offset is not None else 0)
        else:
            # Straight shape: keep latitude constant
            lat = road_centre_lat
        
        lon_base_position= i * m_to_lat + (lon_offset if lon_offset is not None else 0)

        nodes += f"""<node id="{node_id}" lat="{lat:.8f}" lon="{lon_base_position:.8f}" visible="true" version="1"/>\n"""
        road_node_ids.append(node_id)
        node_id += 1
        
        # Increment position by node_sampling for straight or sinusoidal roads.
        i += node_sampling

# Main road way
ways += f"""  <way id="{way_id}" visible="true" version="1">\n"""
for ref in road_node_ids:
    ways += f"    <nd ref=\"{ref}\"/>\n"
ways += """    <tag k="highway" v="primary"/>
    <tag k="lanes" v="2"/>
    <tag k="lanes:forward" v="1"/>
    <tag k="lanes:backward" v="1"/>
    <tag k="cycleway:both" v="lane"/>
    <tag k="sidewalk" v="both"/>
  </way>\n"""

###################
# Output XML file #
###################

xml_footer = "</osm>"

full_xml = xml_header + nodes + ways + xml_footer

with open("TestRoad.osm", "w") as f:
    f.write(full_xml)

print("File 'TestRoad.osm' generated successfully.")